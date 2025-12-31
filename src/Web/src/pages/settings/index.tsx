import Connections from "./Connections";
import ApiTokens from "./ApiTokens";

export default function Settings() {
  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-6">
      <header>
        <h1 className="text-xl font-semibold">
          Settings
        </h1>
      </header>

      <div className="flex flex-col gap-6">
        <Connections />
        <ApiTokens />
      </div>
    </main>

  );
}
