import { redirect } from "next/navigation";

export default function Default() {
  redirect("/login");
  return null;
}
