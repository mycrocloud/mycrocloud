import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Cloud, Zap, Shield, Code2, ArrowRight, Loader2 } from "lucide-react";

const features = [
  {
    icon: Zap,
    title: "Lightning Fast",
    description: "Deploy your APIs instantly with zero configuration",
  },
  {
    icon: Shield,
    title: "Secure by Default",
    description: "Built-in authentication and rate limiting",
  },
  {
    icon: Code2,
    title: "Developer First",
    description: "Simple APIs and powerful routing capabilities",
  },
];

function Home() {
  const { isAuthenticated, isLoading, loginWithRedirect } = useAuth0();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) {
      navigate("apps");
    }
  }, [isAuthenticated, navigate]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="flex flex-col">
      {/* Hero Section */}
      <section className="flex flex-col items-center justify-center px-4 py-20 text-center">
        <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10">
          <Cloud className="h-8 w-8 text-primary" />
        </div>
        <h1 className="max-w-3xl text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl">
          Build and deploy APIs{" "}
          <span className="text-primary">in seconds</span>
        </h1>
        <p className="mt-6 max-w-2xl text-lg text-muted-foreground">
          MycroCloud is a modern platform for building, deploying, and managing
          your APIs. Focus on your code, we handle the infrastructure.
        </p>
        <div className="mt-10 flex gap-4">
          <Button size="lg" onClick={() => loginWithRedirect()} className="gap-2">
            Get Started
            <ArrowRight className="h-4 w-4" />
          </Button>
          <Button
            size="lg"
            variant="outline"
            onClick={() => window.open("https://docs.mycrocloud.info", "_blank")}
          >
            Documentation
          </Button>
        </div>
      </section>

      {/* Features Section */}
      <section className="border-t bg-muted/30 px-4 py-20">
        <div className="mx-auto max-w-5xl">
          <h2 className="text-center text-2xl font-bold tracking-tight sm:text-3xl">
            Everything you need to ship faster
          </h2>
          <p className="mt-4 text-center text-muted-foreground">
            Powerful features to help you build and scale your APIs
          </p>
          <div className="mt-12 grid gap-8 sm:grid-cols-3">
            {features.map((feature) => {
              const Icon = feature.icon;
              return (
                <div
                  key={feature.title}
                  className="flex flex-col items-center rounded-lg border bg-background p-6 text-center"
                >
                  <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10">
                    <Icon className="h-6 w-6 text-primary" />
                  </div>
                  <h3 className="mt-4 font-semibold">{feature.title}</h3>
                  <p className="mt-2 text-sm text-muted-foreground">
                    {feature.description}
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="px-4 py-20">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">
            Ready to get started?
          </h2>
          <p className="mt-4 text-muted-foreground">
            Join developers who are already building with MycroCloud
          </p>
          <Button
            size="lg"
            onClick={() => loginWithRedirect()}
            className="mt-8 gap-2"
          >
            Start Building
            <ArrowRight className="h-4 w-4" />
          </Button>
        </div>
      </section>
    </div>
  );
}

export default Home;
