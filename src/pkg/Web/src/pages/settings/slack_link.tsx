import { useApiClient } from "@/hooks";
import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";

interface LinkResponse {
    redirect_url: string
}

export default function SlackLink() {
    const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
    const { post } = useApiClient();

    const [searchParams] = useSearchParams();
    const redirectUri = searchParams.get("redirectUri")!;
    const state = searchParams.get("state");

    useEffect(() => {
        if (!isLoading && !isAuthenticated) {
            loginWithRedirect({
                appState: {
                    returnTo: window.location.pathname + window.location.search,
                },
            });
            return;
        }
    }, [isAuthenticated, isLoading]);

    const handleConnect = async () => {
        const res = await post<LinkResponse>(redirectUri, { state });
        
        window.location.href = res.redirect_url;
    };

    if (isLoading) {
        return <div>Loading...</div>
    }

    return (
        <div className="text-center pt-[10rem]">
            <div>
                <p className="text-lg">Connect your MycroCloud account</p>
                <p className="my-2">
                    Please authorize MycroCloud to connect with Slack to get personalized notifications for threads you participate in.
                </p>
            </div>
            <button
            className="bg-black text-white px-4 py-2 rounded-md mt-5"
                onClick={handleConnect}
            >
                Continue with MycroCloud
            </button>
        </div>
    );
}