import BuildSection from "./BuildSection";
import CorsSettingsSection from "./CorsSettingsSection";
import GeneralSection from "./GeneralSection";

export default function AppSettings() {
    return <div className="flex flex-col gap-4">
        <h2 className="text-lg font-semibold text-slate-900">Settings</h2>
        <BuildSection />
        <CorsSettingsSection />
        <GeneralSection />
    </div>
}