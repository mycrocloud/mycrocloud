import { useNavigate } from "react-router-dom";
import { Route, Github } from "lucide-react";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

interface OnboardingModalProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
}

export function OnboardingModal({ open, onOpenChange }: OnboardingModalProps) {
    const navigate = useNavigate();

    const handleAction = (path: string) => {
        onOpenChange(false);
        navigate(path);
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-xl">
                <DialogHeader>
                    <DialogTitle className="text-xl">Get started with your app</DialogTitle>
                    <DialogDescription>
                        Your app supports both <strong>hosted web pages</strong> and <strong>serverless API functions</strong>.
                        <br />
                        Select a path to start your onboarding. You can access all features at any time.
                    </DialogDescription>
                </DialogHeader>

                <div className="grid gap-4 py-4 md:grid-cols-2">
                    <button
                        className="flex flex-col items-start gap-4 rounded-xl border bg-card p-6 text-left transition-all hover:border-primary hover:shadow-md focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
                        onClick={() => handleAction("routes/new")}
                    >
                        <div className="rounded-full bg-primary/10 p-3">
                            <Route className="h-6 w-6 text-primary" />
                        </div>
                        <div className="space-y-1">
                            <h3 className="font-semibold leading-none tracking-tight">
                                Create HTTP Route
                            </h3>
                            <p className="text-sm text-muted-foreground">
                                Create a serverless function to handle API requests.
                            </p>
                        </div>
                    </button>

                    <button
                        className="flex flex-col items-start gap-4 rounded-xl border bg-card p-6 text-left transition-all hover:border-primary hover:shadow-md focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2"
                        onClick={() => handleAction("settings/pages")}
                    >
                        <div className="rounded-full bg-primary/10 p-3">
                            <Github className="h-6 w-6 text-primary" />
                        </div>
                        <div className="space-y-1">
                            <h3 className="font-semibold leading-none tracking-tight">
                                Connect GitHub Repo
                            </h3>
                            <p className="text-sm text-muted-foreground">
                                Connect a repository to automatically build and deploy your website.
                            </p>
                        </div>
                    </button>
                </div>

                <div className="flex justify-center">
                    <Button variant="ghost" size="sm" onClick={() => onOpenChange(false)}>
                        I'll explore on my own
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    );
}
