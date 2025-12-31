import CorsSettingsSection from "./CorsSettingsSection";
import GeneralSection from "./GeneralSection";

export default function AppSettings() {
    return <div className="flex flex-col gap-4">
        <CorsSettingsSection />
        <GeneralSection />
    </div>
}