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
import { cn } from "@/lib/utils";


interface OnboardingAction {
    title: string;
    description: string;
    icon: any;
    path: string;
}

interface OnboardingConfig {
    title: string;
    description: string;
    primaryAction: OnboardingAction;
    secondaryAction: OnboardingAction | null;
}

interface OnboardingModalProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    type?: string | null;
}

const QUICK_START_CONFIG: Record<string, OnboardingConfig> = {

    SPA: {
        title: "Your SPA is Ready!",
        description: "Connect your repository to deploy.",
        primaryAction: {
            title: "Connect GitHub Repository",
            description: "Automatic builds and deployments for React, Vue, Next.js, etc.",
            icon: Github,
            path: "settings/pages"
        },
        secondaryAction: null
    },
    API: {
        title: "Your API is Ready!",
        description: "Create your first endpoint.",
        primaryAction: {
            title: "Create HTTP Route",
            description: "Define an endpoint and code your serverless function logic.",
            icon: Route,
            path: "api/routes/new"
        },
        secondaryAction: null
    },
    FullStack: {
        title: "Your App is Ready!",
        description: "Choose where to start:",
        primaryAction: {
            title: "Connect Frontend",
            description: "Link a GitHub repo for your web pages.",
            icon: Github,
            path: "settings/pages"
        },
        secondaryAction: {
            title: "Setup API",
            description: "Create serverless routes for your backend.",
            icon: Route,
            path: "api/routes/new"
        }
    }
};

export function OnboardingModal({ open, onOpenChange, type }: OnboardingModalProps) {
    const navigate = useNavigate();
    const appType = type || "FullStack";

    const handleAction = (path: string) => {
        onOpenChange(false);
        navigate(path);
    };

    const config = QUICK_START_CONFIG[appType] || QUICK_START_CONFIG.FullStack;


    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-2xl gap-0 p-0 overflow-hidden border-none shadow-2xl">
                <div className="bg-gradient-to-br from-primary/10 via-background to-background p-8">
                    <DialogHeader className="mb-8">
                        <div className="flex items-center gap-2 mb-2">
                            <div className="px-2 py-0.5 rounded-full bg-primary/20 text-primary text-[10px] font-bold tracking-wider uppercase">
                                Quick Start Guide
                            </div>
                        </div>
                        <DialogTitle className="text-3xl font-bold tracking-tight">{config.title}</DialogTitle>
                        <DialogDescription className="text-base text-muted-foreground max-w-md">
                            {config.description}
                        </DialogDescription>
                    </DialogHeader>

                    <div className={cn(
                        "grid gap-4 transition-all duration-300",
                        config.secondaryAction ? "md:grid-cols-2" : "md:grid-cols-1"
                    )}>
                        <OnboardingCard
                            {...config.primaryAction}
                            onClick={() => handleAction(config.primaryAction.path)}
                            featured={!config.secondaryAction}
                        />
                        {config.secondaryAction && (
                            <OnboardingCard
                                title={config.secondaryAction.title}
                                description={config.secondaryAction.description}
                                icon={config.secondaryAction.icon}
                                onClick={() => handleAction(config.secondaryAction!.path)}
                            />
                        )}
                    </div>
                </div>

                <div className="bg-muted/30 p-4 border-t flex justify-end">
                    <Button
                        variant="ghost"
                        size="sm"
                        className="text-muted-foreground hover:text-foreground"
                        onClick={() => onOpenChange(false)}
                    >
                        Skip for now
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    );
}

function OnboardingCard({
    title,
    description,
    icon: Icon,
    onClick,
    featured
}: {
    title: string;
    description: string;
    icon: any;
    onClick: () => void;
    featured?: boolean;
}) {
    return (
        <button
            onClick={onClick}
            className={cn(
                "group relative flex flex-col items-start gap-4 rounded-2xl border bg-card p-6 text-left transition-all",
                "hover:border-primary hover:shadow-xl hover:-translate-y-1 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2",
                featured && "md:p-8 border-primary/50 bg-primary/[0.02]"
            )}
        >
            <div className={cn(
                "rounded-xl bg-primary/10 p-3 transition-colors group-hover:bg-primary/20",
                featured && "p-4"
            )}>
                <Icon className={cn("h-6 w-6 text-primary", featured && "h-8 w-8")} />
            </div>
            <div className="space-y-1">
                <h3 className={cn("font-bold leading-none tracking-tight text-lg", featured && "text-xl")}>
                    {title}
                </h3>
                <p className={cn("text-sm text-muted-foreground", featured && "text-base")}>
                    {description}
                </p>
            </div>

            <div className="absolute top-4 right-4 opacity-0 transition-opacity group-hover:opacity-100">
                <div className="rounded-full bg-primary p-1">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className="text-primary-foreground"><path d="M5 12h14M12 5l7 7-7 7" /></svg>
                </div>
            </div>
        </button>
    );
}
