import { useAuth0 } from "@auth0/auth0-react";
import { Spinner, Button } from "flowbite-react";
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

function Home() {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate("/apps", { replace: true });
    }
  }, [isAuthenticated, isLoading, navigate]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Spinner size="xl" />
      </div>
    );
  }

  // Authenticated users will be redirected by useEffect
  // Show loading state to prevent flash of login screen
  if (isAuthenticated) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Spinner size="xl" />
      </div>
    );
  }

  // Public landing page for unauthenticated users
  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-6">
      <div className="text-center max-w-2xl px-4">
        <h1 className="text-4xl font-bold mb-4">Welcome to MycroCloud</h1>
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-8">
          Your cloud platform for building and deploying applications.
        </p>
        <Button size="lg" color="blue" onClick={() => loginWithRedirect()}>
          Get Started
        </Button>
      </div>
    </div>
  );
}
export default Home;
