import Link from "./Link";
import BuildSettings from "./BuildSettings";
import Builds from "./Builds";

export default function Integrations() {
  return (
    <div className="p-2">
      <h1 className="font-bold">Integrations</h1>
      <section>
        <Link />
      </section>
      <section>
        <BuildSettings />
      </section>
      <section>
        <Builds />
      </section>
    </div>
  );
}
