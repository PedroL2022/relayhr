const jsonHeaders = {
  "Content-Type": "application/json"
};

async function readApiResponse(response) {
  const contentType = response.headers.get("content-type") || "";
  const body = contentType.includes("application/json")
    ? await response.json()
    : null;

  if (!response.ok) {
    const validationErrors = body?.errors
      ? Object.values(body.errors).flat().join(" ")
      : "";
    throw new Error(
      validationErrors ||
      body?.detail ||
      body?.title ||
      "No se pudo completar la solicitud. Intenta de nuevo."
    );
  }

  return body;
}

function showNotice(element, message, kind = "info") {
  if (!element) {
    return;
  }

  element.textContent = message;
  element.dataset.kind = kind;
  element.hidden = false;
}

function setBusy(button, busy, busyText) {
  if (!button) {
    return;
  }

  if (busy) {
    button.dataset.originalContent = button.innerHTML;
    button.textContent = busyText;
    button.disabled = true;
  } else {
    button.innerHTML = button.dataset.originalContent || button.innerHTML;
    button.disabled = false;
    delete button.dataset.originalContent;
  }
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: jsonHeaders,
    credentials: "same-origin",
    body: JSON.stringify(payload)
  });

  return readApiResponse(response);
}

const loginForm = document.querySelector("#login-form");
loginForm?.addEventListener("submit", async event => {
  event.preventDefault();
  const button = loginForm.querySelector("button[type='submit']");
  const notice = document.querySelector("#login-message");
  const formData = new FormData(loginForm);
  setBusy(button, true, "Validando acceso...");

  try {
    const result = await postJson("/api/auth/login", {
      email: formData.get("email"),
      password: formData.get("password")
    });
    showNotice(notice, result.message, "success");
    window.location.reload();
  } catch (error) {
    showNotice(notice, error.message, "error");
  } finally {
    setBusy(button, false);
  }
});

const logoutForm = document.querySelector("#logout-form");
logoutForm?.addEventListener("submit", async event => {
  event.preventDefault();
  const button = logoutForm.querySelector("button[type='submit']");
  const notice = document.querySelector("#logout-message");
  setBusy(button, true, "Cerrando sesión...");

  try {
    await postJson("/api/auth/logout", {});
    window.location.reload();
  } catch (error) {
    showNotice(notice, error.message, "error");
    setBusy(button, false);
  }
});

const checklistResult = document.querySelector("#checklist-result");
const checklistNotice = document.querySelector("#checklist-message");

function appendTextElement(parent, tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) {
    element.className = className;
  }
  element.textContent = text;
  parent.append(element);
  return element;
}

function renderChecklist(response) {
  checklistResult.replaceChildren();

  const section = document.createElement("section");
  section.className = "checklist-result";

  const heading = document.createElement("div");
  heading.className = "result-heading";
  const details = document.createElement("div");

  appendTextElement(details, "h3", "", "Tareas del checklist");
  const identifiers = document.createElement("div");
  identifiers.className = "result-identifiers";
  appendTextElement(
    identifiers,
    "span",
    "identifier-chip",
    `Salida #${response.data.employeeExitId}`
  );
  appendTextElement(
    identifiers,
    "span",
    "identifier-chip",
    `Checklist #${response.data.checklistId}`
  );
  details.append(identifiers);
  heading.append(details);
  appendTextElement(
    heading,
    "span",
    "task-count",
    `${response.data.tasks.length} ${response.data.tasks.length === 1 ? "tarea" : "tareas"}`
  );
  section.append(heading);

  const taskList = document.createElement("div");
  taskList.className = "task-list";

  for (const task of response.data.tasks) {
    const card = document.createElement("article");
    card.className = "task-card";
    const main = document.createElement("div");
    main.className = "task-main";
    appendTextElement(main, "p", "task-title", task.title);

    const meta = document.createElement("div");
    meta.className = "task-meta";
    appendTextElement(meta, "span", "task-department", task.department);
    appendTextElement(meta, "span", "", `Responsable: ${task.responsibleName} (#${task.responsibleUserId})`);
    appendTextElement(meta, "span", "", `Fecha límite: ${formatDate(task.dueDate)}`);
    main.append(meta);

    const status = document.createElement("span");
    const statusClass = {
      Pending: "status-pending",
      InProgress: "status-inprogress",
      Completed: "status-completed"
    }[task.status] || "status-pending";
    status.className = `task-status ${statusClass}`;
    status.textContent = {
      Pending: "Pendiente",
      InProgress: "En proceso",
      Completed: "Completada"
    }[task.status] || task.status;

    card.append(main, status);
    taskList.append(card);
  }

  section.append(taskList);
  checklistResult.append(section);
}

function formatDate(value) {
  const parts = value.split("-").map(Number);
  if (parts.length !== 3 || parts.some(Number.isNaN)) {
    return value;
  }

  return new Intl.DateTimeFormat("es", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC"
  }).format(new Date(Date.UTC(parts[0], parts[1] - 1, parts[2])));
}

async function loadChecklist(employeeExitId) {
  const response = await fetch(
    `/api/employee-exits/${encodeURIComponent(employeeExitId)}/checklist`,
    {
      method: "GET",
      credentials: "same-origin",
      headers: { Accept: "application/json" }
    }
  );

  return readApiResponse(response);
}

const lookupForm = document.querySelector("#lookup-form");
lookupForm?.addEventListener("submit", async event => {
  event.preventDefault();
  const button = lookupForm.querySelector("button[type='submit']");
  const employeeExitId = new FormData(lookupForm).get("employeeExitId");
  setBusy(button, true, "Consultando...");

  try {
    const response = await loadChecklist(employeeExitId);
    renderChecklist(response);
    showNotice(checklistNotice, response.message, "success");
  } catch (error) {
    checklistResult.replaceChildren();
    showNotice(checklistNotice, error.message, "error");
  } finally {
    setBusy(button, false);
  }
});

const exitForm = document.querySelector("#exit-form");
exitForm?.addEventListener("submit", async event => {
  event.preventDefault();
  const button = exitForm.querySelector("button[type='submit']");
  const formData = new FormData(exitForm);
  const employeeId = Number(formData.get("employeeId"));
  const effectiveDate = formData.get("effectiveDate");
  const exitNotice = document.querySelector("#exit-message");
  setBusy(button, true, "Creando checklist...");

  try {
    const response = await postJson("/api/employee-exits", {
      employeeId,
      effectiveDate
    });
    const { employeeExitId, checklistId } = response.data;
    showNotice(
      exitNotice,
      `${response.message} Salida #${employeeExitId}, checklist #${checklistId}.`,
      "success"
    );

    const lookupId = document.querySelector("#exit-lookup-id");
    lookupId.value = employeeExitId;
    const checklist = await loadChecklist(employeeExitId);
    renderChecklist(checklist);
    showNotice(checklistNotice, checklist.message, "success");
    document.querySelector("#checklist-panel")
      .scrollIntoView({ behavior: "smooth", block: "start" });
  } catch (error) {
    showNotice(exitNotice, error.message, "error");
  } finally {
    setBusy(button, false);
  }
});

const effectiveDateInput = document.querySelector("#effective-date");
if (effectiveDateInput) {
  effectiveDateInput.min = new Date().toISOString().slice(0, 10);
  effectiveDateInput.value = effectiveDateInput.min;
}
