(() => {
  "use strict";
  const labels = { JobTitle: "Cargo", DepartmentId: "Departamento", JobDescription: "Descripción del puesto", Requirements: "Requisitos" };
  const states = { PendingReview: "Pendiente de revisión", Published: "Publicada", InReview: "En revisión", Approved: "Aprobada", Discarded: "Descartada" };
  const el = id => document.getElementById(id);
  const date = value => value ? new Date(value).toLocaleString("es") : "—";
  const notice = (text, error = false) => { const n = el("vacancy-message"); n.textContent = text; n.hidden = false; n.dataset.kind = error ? "error" : "success"; };
  const missing = fields => { el("missing-fields").textContent = fields.length ? "Campos pendientes: " + fields.map(f => labels[f] || f).join(", ") : "Todos los campos obligatorios están completos."; };
  async function request(url, options = {}) {
    let response;
    try { response = await fetch(url, { credentials: "same-origin", ...options }); }
    catch { throw new Error("No se pudo conectar. Conservamos los datos del formulario; vuelve a intentarlo."); }
    const body = await response.json().catch(() => null);
    if (!response.ok) {
      const fields = body?.data?.missingFields;
      if (fields && el("missing-fields")) missing(fields);
      const errors = body?.errors || body?.data?.errors;
      const details = errors ? Object.values(errors).flat().join(" ") : "";
      const error = new Error([body?.message || body?.detail || body?.title || `Error ${response.status}`, details, fields?.map(f => labels[f] || f).join(", ")].filter(Boolean).join(" "));
      error.status = response.status;
      throw error;
    }
    return body;
  }
  if (el("vacancy-list")) {
    let page = 1, loading = false;
    async function load() {
      if (loading) return;
      loading = true; el("previous-page").disabled = el("next-page").disabled = true;
      try {
        const status = el("status-filter").value;
        const body = await request(`/api/express-vacancies?page=${page}&pageSize=20${status ? `&status=${status}` : ""}`);
        const result = body.data, rows = el("vacancy-rows"); rows.replaceChildren();
        result.items.forEach(v => {
          const tr = document.createElement("tr");
          [v.expressVacancyId, v.jobTitle === "ToComplete" ? "Pendiente de completar" : v.jobTitle, v.department === "ToComplete" ? "Pendiente de completar" : v.department, states[v.status] || v.status, date(v.createdAt), v.missingFields.map(f => labels[f] || f).join(", ") || "Ninguno", date(v.publishedAt)].forEach(text => { const td = document.createElement("td"); td.textContent = text; tr.append(td); });
          const td = document.createElement("td"), link = document.createElement("a"); link.href = `/Vacancies/Review/${v.expressVacancyId}`; link.textContent = "Revisar"; td.append(link); tr.append(td); rows.append(tr);
        });
        if (!result.items.length) notice("No hay vacantes para este filtro."); else el("vacancy-message").hidden = true;
        el("page-label").textContent = `Página ${page} · ${result.totalCount} vacantes`;
        el("previous-page").disabled = page === 1;
        el("next-page").disabled = page * 20 >= result.totalCount;
      } catch (e) { notice(e.message, true); }
      finally { loading = false; }
    }
    el("status-filter").addEventListener("change", () => { page = 1; load(); });
    el("previous-page").addEventListener("click", () => { page--; load(); });
    el("next-page").addEventListener("click", () => { page++; load(); });
    load();
  }
  if (el("vacancy-review")) {
    const id = el("vacancy-review").dataset.id, base = `/api/express-vacancies/${id}`;
    let current, busy = false, locked = false;
    const formData = () => ({ jobTitle: el("job-title").value, departmentId: el("department").value ? Number(el("department").value) : null, jobDescription: el("job-description").value, requirements: el("requirements").value });
    const headers = () => ({ "Content-Type": "application/json", "X-CSRF-TOKEN": document.querySelector('input[name="__RequestVerificationToken"]').value });
    function controls() {
      const disabled = busy || locked || !current || current.status !== "PendingReview";
      el("vacancy-fields").disabled = disabled; el("save-vacancy").disabled = disabled;
      if (el("publish-vacancy")) { el("publish-vacancy").disabled = disabled; el("publish-vacancy").hidden = current && current.status !== "PendingReview"; }
    }
    function render(v) {
      current = v;
      el("job-title").value = v.jobTitle === "ToComplete" ? "" : v.jobTitle || "";
      el("department").value = v.departmentId || "";
      el("job-description").value = v.jobDescription === "ToComplete" ? "" : v.jobDescription || "";
      el("requirements").value = v.requirements === "ToComplete" ? "" : v.requirements || "";
      el("vacancy-summary").textContent = `${states[v.status] || v.status} · Salida original #${v.employeeExitId} · Creación: ${date(v.createdAt)}`;
      el("vacancy-audit").textContent = v.publishedAt ? `Publicada: ${date(v.publishedAt)} · Usuario responsable #${v.publishedByUserId}` : v.updatedAt ? `Última edición: ${date(v.updatedAt)} · Usuario #${v.updatedByUserId}` : "";
      el("vacancy-metric").textContent = v.publishedBeforeEffectiveExit ? "Publicada antes de la fecha efectiva de salida. El plazo aún no había comenzado." : v.hoursToPublication != null ? `${v.hoursToPublication.toFixed(2)} horas desde las 00:00 UTC de la fecha efectiva de salida · ${v.meets48HourTarget ? "Meta de menos de 48 horas cumplida" : "Meta de 48 horas superada"}` : "El plazo se mide desde las 00:00 UTC de la fecha efectiva de salida.";
      missing(v.missingFields); el("public-vacancy-link").hidden = v.status !== "Published"; controls();
    }
    async function save() { const body = await request(base, { method: "PUT", headers: headers(), body: JSON.stringify(formData()) }); render(body.data); return body; }
    async function run(action) {
      if (busy) return;
      busy = true; controls();
      try { await action(); }
      catch (e) { notice(e.message, true); if (e.status === 409) { locked = true; notice(e.message + " Recargue para ver la versión actual.", true); } }
      finally { busy = false; controls(); }
    }
    el("vacancy-form").addEventListener("submit", e => { e.preventDefault(); run(async () => { await save(); notice("Cambios guardados correctamente."); }); });
    el("publish-vacancy")?.addEventListener("click", () => { if (!busy) el("publish-confirmation").showModal(); });
    el("cancel-publish").addEventListener("click", () => el("publish-confirmation").close());
    el("confirm-publish").addEventListener("click", () => { el("publish-confirmation").close(); run(async () => { await save(); const body = await request(base + "/publish", { method: "POST", headers: headers() }); render(body.data); notice(body.message); }); });
    run(async () => {
      const [departments, body] = await Promise.all([request("/api/express-vacancies/departments"), request(base)]);
      departments.forEach(d => { const option = document.createElement("option"); option.value = d.id; option.textContent = d.name; el("department").append(option); }); render(body.data);
    });
  }
})();
