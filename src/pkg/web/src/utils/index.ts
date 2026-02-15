export async function downloadFile(
  url: string,
  headers: Record<string, string>,
  name: string,
) {
  const res = await fetch(url, { headers });
  if (res.ok) {
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = name;
    a.click();
  }
}
