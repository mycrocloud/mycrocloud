import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";

export default function SlackLink() {
    console.log("SlackLink")
    const { isAuthenticated, isLoading, loginWithRedirect, getAccessTokenSilently, user } = useAuth0();

    const [searchParams] = useSearchParams();
    const redirectUri = searchParams.get("redirectUri") || "/api/intergations/slack/link-callback";
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
        try {
            const token = await getAccessTokenSilently({
                //authorizationParams: { audience: "slack-integration-api" }
            });

            const res = await fetch(redirectUri, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ state }),
            });

            if (res.ok) {
                alert(`✅ Slack linked successfully as ${user?.email}`);
            } else {
                const err = await res.text();
                alert("⚠️ Failed to link Slack: " + err);
            }
        } catch (e) {
            console.error(e);
            alert("Error linking Slack");
        }
    };

    if (isLoading) {
        return <div>Loading...</div>
    }

    return (
        <div style={{ textAlign: "center", paddingTop: "5rem" }}>
            <h2>Connect your MyHub account</h2>
            <button
                style={{
                    background: "black",
                    color: "white",
                    padding: "10px 20px",
                    borderRadius: "6px",
                    cursor: "pointer",
                }}
                onClick={handleConnect}
            >
                Continue with MycroCloud
            </button>
        </div>
    );
}