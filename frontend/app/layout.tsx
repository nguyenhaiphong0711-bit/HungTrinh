import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = {title: "HungTrinh · Quản lý", description: "Quản lý sản phẩm và danh mục"};
export default function RootLayout({children}: Readonly<{children: React.ReactNode}>) {
  return <html lang="vi"><body>{children}</body></html>;
}
