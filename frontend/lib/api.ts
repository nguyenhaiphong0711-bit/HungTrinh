let csrf: string | null = null;
export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = (options.method ?? "GET").toUpperCase();
  const headers = new Headers(options.headers);
  if (options.body) headers.set("Content-Type", "application/json");
  if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
    if (!csrf) {
      const response = await fetch("/api/auth/csrf", {credentials:"same-origin", cache:"no-store"});
      if (!response.ok) throw new Error("Không lấy được mã bảo vệ phiên. Vui lòng thử lại.");
      csrf = (await response.json()).token;
    }
    if (!csrf) throw new Error("Mã bảo vệ phiên không hợp lệ.");
    headers.set("X-CSRF-TOKEN", csrf);
  }
  const response = await fetch(`/api${path}`, {...options, headers, credentials:"same-origin", cache:"no-store"});
  if (!response.ok) {
    if (response.status === 401) throw new Error("Vui lòng đăng nhập để tiếp tục.");
    if (response.status === 403) throw new Error("Tài khoản không có quyền thực hiện thao tác này.");
    const body = await response.json().catch(() => null);
    const details = body?.errors ? Object.values(body.errors).flat().join(" ") : null;
    throw new Error(details || body?.detail || body?.title || body?.message || "Thao tác thất bại. Vui lòng thử lại.");
  }
  if (path === "/auth/login" || path === "/auth/logout" || path === "/auth/register") csrf = null;
  if (response.status === 204 || response.headers.get("content-length") === "0") return undefined as T;
  const text = await response.text();
  return text ? JSON.parse(text) as T : undefined as T;
}
