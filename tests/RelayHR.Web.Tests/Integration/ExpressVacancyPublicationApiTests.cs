using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;
using RelayHR.Web.Services;

namespace RelayHR.Web.Tests.Integration;
public sealed class ExpressVacancyPublicationApiTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    internal static async Task<int> SeedVacancy(TestApplication app, bool complete = true, DateOnly? effectiveDate = null)
        => await app.InScopeAsync(async db =>
        {
            var exit = new EmployeeExit { EmployeeId = 15, EffectiveDate = effectiveDate ?? Today };
            var vacancy = ExpressVacancy.CreateFromExit(exit, null, DateTime.UtcNow);
            if (complete)
            {
                vacancy.JobTitle = "Analista"; vacancy.DepartmentId = 1;
                vacancy.JobDescription = "Coordinar operaciones"; vacancy.Requirements = "Experiencia en RRHH";
                vacancy.MissingFields = vacancy.CalculateMissingFields();
            }
            db.ExpressVacancies.Add(vacancy); await db.SaveChangesAsync(); return vacancy.ExpressVacancyId;
        });
    internal static async Task<string> Token(HttpClient client, int id)
    {
        var response = await client.GetAsync($"/Vacancies/Review/{id}"); response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "La vista debe generar token CSRF.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    internal static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, int id,
        string json = "{}", bool publish = false, bool csrf = true)
    {
        using var request = new HttpRequestMessage(method, $"/api/express-vacancies/{id}" + (publish ? "/publish" : ""));
        if (csrf) request.Headers.Add("X-CSRF-TOKEN", await Token(client, id));
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(); return json.GetProperty("data");
    }
    [Fact]
    public async Task Partial_edit_preserves_omitted_values_relation_and_records_audit()
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        var before = await app.InScopeAsync(db => db.ExpressVacancies.AsNoTracking().SingleAsync());
        using var response = await Send(app.Client, HttpMethod.Put, id, "{\"jobTitle\":\"  Coordinador  \"}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await app.InScopeAsync(db => db.ExpressVacancies.AsNoTracking().SingleAsync());
        Assert.Equal("Coordinador", after.JobTitle); Assert.Equal(before.JobDescription, after.JobDescription);
        Assert.Equal(before.Requirements, after.Requirements); Assert.Equal(before.DepartmentId, after.DepartmentId);
        Assert.Equal(before.EmployeeExitId, after.EmployeeExitId); Assert.Equal(104, after.UpdatedByUserId);
        Assert.NotNull(after.UpdatedAt); Assert.NotEqual(before.Revision, after.Revision);
    }
    [Fact]
    public async Task Explicit_null_clears_field_and_completing_it_recalculates_pending_fields()
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var cleared = await Send(app.Client, HttpMethod.Put, id, "{\"requirements\":null}");
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Contains("Requirements", (await Data(cleared)).GetProperty("missingFields").EnumerateArray().Select(x => x.GetString()));
        using var completed = await Send(app.Client, HttpMethod.Put, id, "{\"requirements\":\"Experiencia\"}");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Empty((await Data(completed)).GetProperty("missingFields").EnumerateArray());
    }
    [Theory]
    [InlineData("{\"departmentId\":999}")]
    [InlineData("{\"status\":\"Published\"}")]
    [InlineData("{\"updatedByUserId\":101}")]
    [InlineData("{\"employeeExitId\":88}")]
    public async Task Rejects_invalid_references_and_internal_fields(string json)
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var response = await Send(app.Client, HttpMethod.Put, id, json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Theory]
    [InlineData("jobTitle",151)] [InlineData("jobDescription",4001)] [InlineData("requirements",4001)]
    public async Task Rejects_overlong_values(string field, int length)
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var response = await Send(app.Client, HttpMethod.Put, id, JsonSerializer.Serialize(new Dictionary<string,string>{{field,new string('x',length)}}));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Publication_lists_missing_fields_and_does_not_change_state()
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app, false);
        using var response = await Send(app.Client, HttpMethod.Post, id, publish:true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(4, (await Data(response)).GetProperty("missingFields").GetArrayLength());
        var vacancy = await app.InScopeAsync(db => db.ExpressVacancies.AsNoTracking().SingleAsync());
        Assert.Equal(ExpressVacancyStatus.PendingReview, vacancy.Status); Assert.Null(vacancy.PublishedAt);
    }
    [Fact]
    public async Task Whitespace_or_marker_is_not_publishable()
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var edit = await Send(app.Client, HttpMethod.Put, id, JsonSerializer.Serialize(new { jobTitle = "   ", requirements = "ToComplete" }));
        edit.EnsureSuccessStatusCode();
        using var response = await Send(app.Client, HttpMethod.Post, id, publish:true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, (await Data(response)).GetProperty("missingFields").GetArrayLength());
    }
    [Fact]
    public async Task Publishes_once_records_identity_and_exposes_only_public_fields()
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var response = await Send(app.Client, HttpMethod.Post, id, publish:true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await Data(response); Assert.Equal("Published", data.GetProperty("status").GetString());
        Assert.Equal(104,data.GetProperty("publishedByUserId").GetInt32()); Assert.True(data.GetProperty("publishedAt").GetDateTime().Kind == DateTimeKind.Utc);
        using var repeated = await Send(app.Client, HttpMethod.Post, id, publish:true);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        using var edit = await Send(app.Client, HttpMethod.Put, id,"{\"jobTitle\":\"Otro\"}");
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        var stored = await app.InScopeAsync(db => db.ExpressVacancies.AsNoTracking().SingleAsync());
        Assert.Equal(data.GetProperty("publishedAt").GetDateTime(),stored.PublishedAt); Assert.Equal(104,stored.PublishedByUserId);
        using var anonymous = app.CreateUnauthenticatedClient();
        using var publicResponse = await anonymous.GetAsync($"/api/public/vacancies/{id}");
        publicResponse.EnsureSuccessStatusCode();
        Assert.Equal(new[]{"department","id","jobDescription","jobTitle","publishedAt","requirements"},
            (await Data(publicResponse)).EnumerateObject().Select(p=>p.Name).OrderBy(n=>n).ToArray());
        var html = await anonymous.GetStringAsync($"/vacancies/{id}");
        Assert.Contains("Analista",html); Assert.DoesNotContain("Salida original",html);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Anonymous_or_wrong_role_cannot_modify(bool wrongRole)
    {
        await using var app = await TestApplication.StartAsync(); var id = await SeedVacancy(app);
        using var anonymous = app.CreateUnauthenticatedClient();
        if(wrongRole) await app.LoginAsync("employee@example.test");
        var client=wrongRole?app.Client:anonymous;
        foreach(var publish in new[]{false,true})
        {
            using var response=await Send(client,publish?HttpMethod.Post:HttpMethod.Put,id,publish:publish,csrf:false);
            Assert.Equal(wrongRole?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized,response.StatusCode);
        }
    }
    [Fact]
    public async Task Recruiter_can_edit_but_cannot_publish()
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        using(var scope=app.Factory.Services.CreateScope())
        {
            var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            Assert.True((await roles.CreateAsync(new IdentityRole<int>(Roles.Recruiter))).Succeeded);
            var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync("employee@example.test"))!,Roles.Recruiter)).Succeeded);
        }
        await app.LoginAsync("employee@example.test");
        using var edit=await Send(app.Client,HttpMethod.Put,id,"{\"jobTitle\":\"Coordinador\"}");
        Assert.Equal(HttpStatusCode.OK,edit.StatusCode);
        using var publish=await Send(app.Client,HttpMethod.Post,id,publish:true);
        Assert.Equal(HttpStatusCode.Forbidden,publish.StatusCode);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Cookie_mutations_require_antiforgery(bool publish)
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        using var response=await Send(app.Client,publish?HttpMethod.Post:HttpMethod.Put,id,publish:publish,csrf:false);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
    [Fact]
    public async Task Pending_and_unknown_vacancies_are_not_public()
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        using var client=app.CreateUnauthenticatedClient();
        foreach(var candidateId in new[]{id,999})
        {
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/public/vacancies/{candidateId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/vacancies/{candidateId}")).StatusCode);
        }
    }
    [Fact]
    public async Task Unknown_internal_vacancy_returns_not_found()
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        var token=await Token(app.Client,id);
        foreach(var suffix in new[]{"","/publish"})
        {
            using var request=new HttpRequestMessage(suffix==""?HttpMethod.Put:HttpMethod.Post,"/api/express-vacancies/999"+suffix);
            request.Headers.Add("X-CSRF-TOKEN",token); request.Content=JsonContent.Create(new{});
            using var response=await app.Client.SendAsync(request); Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        }
    }
    [Fact]
    public async Task Razor_pages_render_and_encode_untrusted_text()
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        var list=await app.Client.GetStringAsync("/Vacancies"); Assert.Contains("Vacantes Express",list);
        var review=await app.Client.GetStringAsync($"/Vacancies/Review/{id}"); Assert.Contains("Publicar vacante",review);
        using var edit=await Send(app.Client,HttpMethod.Put,id,"{\"jobTitle\":\"<script>alert(1)</script>\"}"); edit.EnsureSuccessStatusCode();
        using var publish=await Send(app.Client,HttpMethod.Post,id,publish:true); publish.EnsureSuccessStatusCode();
        var html=await app.Client.GetStringAsync($"/vacancies/{id}"); Assert.DoesNotContain("<script>alert(1)</script>",html); Assert.Contains("&lt;script&gt;",html);
    }
    [Fact]
    public async Task Exit_registration_enqueues_HU04_and_generated_vacancy_can_be_completed_and_published()
    {
        await using var app=await TestApplication.StartAsync();
        using var exit=await app.Client.PostAsJsonAsync("/api/employee-exits",new {employeeId=15,effectiveDate=Today});
        Assert.Equal(HttpStatusCode.Created,exit.StatusCode);
        using(var scope=app.Factory.Services.CreateScope())
            Assert.Equal(1,await scope.ServiceProvider.GetRequiredService<IExpressVacancyRequestProcessor>().ProcessPendingAsync(default));
        var id=await app.InScopeAsync(db=>db.ExpressVacancies.Select(v=>v.ExpressVacancyId).SingleAsync());
        using var edit=await Send(app.Client,HttpMethod.Put,id,"{\"jobTitle\":\"Analista\",\"departmentId\":1,\"jobDescription\":\"Operaciones\",\"requirements\":\"Experiencia\"}");
        edit.EnsureSuccessStatusCode();
        using var publish=await Send(app.Client,HttpMethod.Post,id,publish:true); publish.EnsureSuccessStatusCode();
        Assert.Equal("Published",(await Data(publish)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Department_change_is_persisted_and_response_shows_new_department()
    {
        await using var app=await TestApplication.StartAsync(); var id=await SeedVacancy(app);
        using var response=await Send(app.Client,HttpMethod.Put,id,"{\"departmentId\":3}");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal("Finanzas",(await Data(response)).GetProperty("department").GetString());
        Assert.Equal(3,await app.InScopeAsync(db=>db.ExpressVacancies.Select(v=>v.DepartmentId).SingleAsync()));
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Configured_bearer_uses_authenticated_subject_without_cookie_antiforgery(bool configured)
    {
        await using var app=await TestApplication.StartAsync(enableJwt:configured); var id=await SeedVacancy(app);
        var key=new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes("HU05-test-only-signing-key-at-least-32-characters"));
        var token=new System.IdentityModel.Tokens.Jwt.JwtSecurityToken("relayhr-tests","relayhr-tests",
            new[]{new System.Security.Claims.Claim("sub","104"),new System.Security.Claims.Claim("role","HRAdmin")},
            expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:new Microsoft.IdentityModel.Tokens.SigningCredentials(key,Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));
        using var client=app.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token));
        using var edit=await Send(client,HttpMethod.Put,id,"{\"jobTitle\":\"Analista JWT\"}",csrf:false);
        Assert.Equal(configured ? HttpStatusCode.OK : HttpStatusCode.Unauthorized,edit.StatusCode);
        if (!configured) return;
        using var publish=await Send(client,HttpMethod.Post,id,publish:true,csrf:false);
        Assert.Equal(HttpStatusCode.OK,publish.StatusCode);
        Assert.Equal(104,(await Data(publish)).GetProperty("publishedByUserId").GetInt32());
    }
}
