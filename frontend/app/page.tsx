"use client";
import {FormEvent, useCallback, useEffect, useState} from "react";
import {api} from "../lib/api";

type User = {email:string; roles:string[]};
type Category = {id:number; name:string};
type Product = {id:number; name:string; price:number; categoryId:number; stock:number};
type AdminUser = User & {id:string};
type Tab = "products" | "categories" | "admin";
const money = new Intl.NumberFormat("vi-VN", {style:"currency", currency:"VND"});

export default function Home() {
  const [user,setUser] = useState<User|null>(null);
  const [loading,setLoading] = useState(true);
  const [busy,setBusy] = useState(false);
  const [error,setError] = useState("");
  const [notice,setNotice] = useState("");
  const [register,setRegister] = useState(false);
  const [tab,setTab] = useState<Tab>("products");
  const [products,setProducts] = useState<Product[]>([]);
  const [categories,setCategories] = useState<Category[]>([]);
  const [users,setUsers] = useState<AdminUser[]>([]);
  const [search,setSearch] = useState("");
  const [editProduct,setEditProduct] = useState<Product|null>(null);
  const [editCategory,setEditCategory] = useState<Category|null>(null);
  const canWrite = user?.roles.some(role=>["Administrator","Staff"].includes(role));
  const isAdmin = user?.roles.includes("Administrator");

  const refresh = useCallback(async()=> {
    const [p,c] = await Promise.all([api<Product[]>("/products"),api<Category[]>("/categories")]);
    setProducts(p); setCategories(c);
  },[]);
  useEffect(()=>{api<User>("/auth/me").then(setUser).catch(()=>setUser(null)).finally(()=>setLoading(false));},[]);
  useEffect(()=>{if(user) refresh().catch(e=>setError(e.message));},[user,refresh]);
  useEffect(()=>{if(tab==="admin" && isAdmin) api<AdminUser[]>("/admin/users").then(setUsers).catch(e=>setError(e.message));},[tab,isAdmin]);

  async function execute(action:()=>Promise<void>) {
    setBusy(true); setError(""); setNotice("");
    try {await action();} catch(e) {setError(e instanceof Error ? e.message : "Có lỗi xảy ra.");} finally {setBusy(false);}
  }
  function authenticate(event:FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data=new FormData(event.currentTarget);
    void execute(async()=>{
      await api(`/auth/${register?"register":"login"}`,{method:"POST",body:JSON.stringify({email:data.get("email"),password:data.get("password")})});
      if(register){setRegister(false);setNotice("Đăng ký thành công. Hãy đăng nhập bằng tài khoản vừa tạo.");}
      else {setUser(await api<User>("/auth/me"));}
    });
  }
  function saveProduct(event:FormEvent<HTMLFormElement>){
    event.preventDefault();const form=event.currentTarget;const data=new FormData(form);
    void execute(async()=>{
      await api(`/products${editProduct?`/${editProduct.id}`:""}`,{method:editProduct?"PUT":"POST",body:JSON.stringify({name:data.get("name"),price:Number(data.get("price")),categoryId:Number(data.get("categoryId")),stock:Number(data.get("stock"))})});
      setEditProduct(null);form.reset();await refresh();setNotice("Đã lưu sản phẩm.");
    });
  }
  function saveCategory(event:FormEvent<HTMLFormElement>){
    event.preventDefault();const form=event.currentTarget;const data=new FormData(form);
    void execute(async()=>{
      await api(`/categories${editCategory?`/${editCategory.id}`:""}`,{method:editCategory?"PUT":"POST",body:JSON.stringify({name:data.get("name")})});
      setEditCategory(null);form.reset();await refresh();setNotice("Đã lưu danh mục.");
    });
  }
  function remove(kind:"products"|"categories",id:number){
    if(!window.confirm("Bạn chắc chắn muốn xóa mục này?"))return;
    void execute(async()=>{await api(`/${kind}/${id}`,{method:"DELETE"});await refresh();setNotice("Đã xóa.");});
  }
  const messages=<>{error&&<div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-red-800">{error}</div>}{notice&&<div role="status" className="mb-5 rounded-xl border border-green-200 bg-green-50 p-4 text-green-800">{notice}</div>}</>;
  if(loading)return <main className="p-10" role="status">Đang kiểm tra phiên đăng nhập…</main>;
  if(!user)return <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-6 py-12"><p className="mb-3 text-sm font-bold tracking-widest text-blue-700">HUNGTRINH / MANAGEMENT</p><h1 className="mb-2 text-3xl font-bold">{register?"Tạo tài khoản":"Chào mừng trở lại"}</h1><p className="mb-8 text-slate-500">Quản lý sản phẩm và danh mục tại một nơi.</p>{messages}<form onSubmit={authenticate} className="card space-y-5"><label className="block">Email<input className="mt-2 block w-full" name="email" type="email" autoComplete="email" required maxLength={256}/></label><label className="block">Mật khẩu<input className="mt-2 block w-full" name="password" type="password" autoComplete={register?"new-password":"current-password"} required maxLength={128} minLength={register?12:1}/></label>{register&&<p className="text-sm text-slate-500">Ít nhất 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tài khoản mới nhận vai trò User.</p>}<button disabled={busy} className="primary w-full">{busy?"Đang xử lý…":register?"Đăng ký":"Đăng nhập"}</button></form><button className="mt-6 text-sm text-blue-700" onClick={()=>{setRegister(!register);setError("");setNotice("");}}>{register?"Đã có tài khoản? Đăng nhập":"Chưa có tài khoản? Đăng ký"}</button></main>;
  const filtered=products.filter(p=>p.name.toLocaleLowerCase().includes(search.toLocaleLowerCase()));
  return <div className="min-h-screen md:flex"><aside className="border-r border-slate-200 bg-white p-6 md:w-64 md:shrink-0"><p className="mb-1 text-xl font-bold text-blue-700">HungTrinh<span className="text-blue-300">.</span></p><p className="mb-10 text-xs uppercase tracking-widest text-slate-400">Không gian quản lý</p><nav aria-label="Điều hướng chính" className="flex gap-2 md:flex-col">{([["products","Sản phẩm"],["categories","Danh mục"],...(isAdmin?[["admin","Quản trị"]]:[])] as [Tab,string][]).map(([id,label])=><button key={id} aria-current={tab===id?"page":undefined} className={`rounded-lg px-4 py-3 text-left text-sm font-semibold ${tab===id?"bg-blue-50 text-blue-700":"text-slate-500 hover:bg-slate-50"}`} onClick={()=>{setTab(id);setError("");setNotice("");}}>{label}</button>)}</nav><div className="mt-10 border-t border-slate-100 pt-5 text-sm"><p className="break-all">{user.email}</p><p className="mt-1 text-xs text-slate-500">{user.roles.join(", ")}</p><button disabled={busy} className="mt-4 text-blue-700" onClick={()=>void execute(async()=>{await api("/auth/logout",{method:"POST"});setUser(null);setProducts([]);setCategories([]);setUsers([]);setTab("products");})}>Đăng xuất</button></div></aside><main className="min-w-0 flex-1 p-5 md:p-10"><header className="mb-8 flex flex-wrap items-center justify-between gap-4"><div><p className="mb-2 text-sm text-slate-500">Tổng quan / {tab==="products"?"Sản phẩm":tab==="categories"?"Danh mục":"Quản trị"}</p><h1 className="text-3xl font-bold">{tab==="products"?"Quản lý sản phẩm":tab==="categories"?"Quản lý danh mục":"Quản trị tài khoản"}</h1></div><span className="rounded-full border border-blue-100 bg-blue-50 px-4 py-2 text-xs font-semibold text-blue-700">{user.roles.join(" · ")}</span></header>{messages}<section className="mb-8 grid gap-4 sm:grid-cols-3"><div className="card"><p className="text-sm text-slate-500">Sản phẩm</p><p className="mt-3 text-3xl font-bold">{products.length}</p></div><div className="card"><p className="text-sm text-slate-500">Danh mục</p><p className="mt-3 text-3xl font-bold">{categories.length}</p></div><div className="card"><p className="text-sm text-slate-500">Quyền thao tác</p><p className="mt-3 text-lg font-semibold">{canWrite?"Xem và chỉnh sửa":"Chỉ xem dữ liệu"}</p></div></section>
    {tab==="products"&&<>{canWrite&&<form key={editProduct?.id??"new"} onSubmit={saveProduct} className="card mb-6"><h2 className="mb-4 font-semibold">{editProduct?"Chỉnh sửa sản phẩm":"Thêm sản phẩm"}</h2><div className="grid gap-4 lg:grid-cols-5"><label className="text-sm">Tên sản phẩm<input className="mt-2 w-full" name="name" required maxLength={200} defaultValue={editProduct?.name}/></label><label className="text-sm">Giá (VND)<input className="mt-2 w-full" name="price" type="number" min="0" max="999999999" step="0.01" required defaultValue={editProduct?.price}/></label><label className="text-sm">Tồn kho<input className="mt-2 w-full" name="stock" type="number" min="0" max="2147483647" step="1" required defaultValue={editProduct?.stock??0}/></label><label className="text-sm">Danh mục<select className="mt-2 w-full" name="categoryId" defaultValue={editProduct?.categoryId??""} required><option value="" disabled>Chọn danh mục</option>{categories.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></label><div className="flex items-end gap-2"><button disabled={busy||categories.length===0} className="primary">Lưu sản phẩm</button>{editProduct&&<button type="button" className="secondary" onClick={()=>setEditProduct(null)}>Hủy</button>}</div></div>{categories.length===0&&<p className="mt-3 text-sm text-amber-700">Hãy tạo danh mục trước khi thêm sản phẩm.</p>}</form>}<section className="card overflow-hidden !p-0"><div className="flex flex-wrap items-center justify-between gap-3 p-5"><h2 className="font-semibold">Danh sách sản phẩm</h2><input aria-label="Tìm sản phẩm" placeholder="Tìm theo tên…" value={search} onChange={e=>setSearch(e.target.value)}/></div><div className="overflow-x-auto"><table className="w-full"><thead><tr><th>Tên sản phẩm</th><th>Danh mục</th><th>Giá</th><th>Tồn kho</th>{canWrite&&<th>Thao tác</th>}</tr></thead><tbody>{filtered.map(p=><tr key={p.id}><td className="font-medium">{p.name}</td><td>{categories.find(c=>c.id===p.categoryId)?.name??"—"}</td><td>{money.format(p.price)}</td><td>{p.stock}</td>{canWrite&&<td><div className="flex gap-3"><button disabled={busy} className="text-sm text-blue-700" onClick={()=>setEditProduct(p)}>Sửa</button><button disabled={busy} className="text-sm text-red-700" onClick={()=>remove("products",p.id)}>Xóa</button></div></td>}</tr>)}</tbody></table>{filtered.length===0&&<p className="p-8 text-center text-slate-500">Chưa có sản phẩm phù hợp.</p>}</div></section></>}
    {tab==="categories"&&<>{canWrite&&<form key={editCategory?.id??"new"} onSubmit={saveCategory} className="card mb-6"><h2 className="mb-4 font-semibold">{editCategory?"Chỉnh sửa danh mục":"Thêm danh mục"}</h2><div className="flex flex-wrap items-end gap-3"><label className="text-sm">Tên danh mục<input className="mt-2 block" name="name" required maxLength={100} defaultValue={editCategory?.name}/></label><button disabled={busy} className="primary">Lưu danh mục</button>{editCategory&&<button type="button" className="secondary" onClick={()=>setEditCategory(null)}>Hủy</button>}</div></form>}<section className="card overflow-x-auto !p-0"><table className="w-full"><thead><tr><th>Tên danh mục</th>{canWrite&&<th>Thao tác</th>}</tr></thead><tbody>{categories.map(c=><tr key={c.id}><td>{c.name}</td>{canWrite&&<td><div className="flex gap-3"><button disabled={busy} className="text-sm text-blue-700" onClick={()=>setEditCategory(c)}>Sửa</button><button disabled={busy} className="text-sm text-red-700" onClick={()=>remove("categories",c.id)}>Xóa</button></div></td>}</tr>)}</tbody></table>{categories.length===0&&<p className="p-8 text-center text-slate-500">Chưa có danh mục.</p>}</section></>}
    {tab==="admin"&&isAdmin&&<section className="card overflow-x-auto !p-0"><div className="p-5"><h2 className="font-semibold">Tài khoản hệ thống</h2><p className="mt-2 text-sm text-slate-500">Khu vực này dành riêng cho Administrator. Quyền truy cập được kiểm tra tại backend.</p></div><table className="w-full"><thead><tr><th>Email</th><th>Vai trò</th></tr></thead><tbody>{users.map(u=><tr key={u.id}><td>{u.email}</td><td>{u.roles.join(", ")}</td></tr>)}</tbody></table>{users.length===0&&!error&&<p className="p-8 text-center text-slate-500">Chưa có dữ liệu tài khoản.</p>}</section>}
  </main></div>;
}
