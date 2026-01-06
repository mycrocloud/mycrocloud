import BuildSettings from "./BuildSettings";
import GitRepo from "./Link";

export default function BuildSection() {
    return <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
        <header>
            <h3 className="text-base font-semibold">
                Build
            </h3>
        </header>
        <GitRepo />
        <BuildSettings />
    </section>
}