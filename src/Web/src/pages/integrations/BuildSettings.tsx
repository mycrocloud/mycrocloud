import { useApiClient } from "@/hooks";
import { useContext, useEffect } from "react";
import { AppContext } from "../apps";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import InfoIcon from "@/components/ui/InfoIcon";
import { AlertTriangle } from "lucide-react";


export default function BuildSettings() {
    const { get, post } = useApiClient();
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
    } = useForm<BuildConfig>({
        defaultValues: {
            branch: "default",
            directory: ".",
            buildCommand: "npm run build",
            outDir: "dist",
        },
    });

    useEffect(() => {
        (async () => {
            const config = await get<IBuildConfig>(`/api/apps/${app.id}/builds/config`);

            setValue("branch", config.branch);
            setValue("directory", config.directory);
            setValue("buildCommand", config.buildCommand);
            setValue("outDir", config.outDir);
        })();
    }, []);

    const onSubmitConfig = async (data: BuildConfig) => {
        await post(`/api/apps/${app.id}/builds/config`, data);
        toast.success("Build configuration saved");
    };

    return <div className="mt-4">
        <h2 className="font-semibold mb-3">Build Settings</h2>
        <Alert variant="destructive" className="mb-4">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
                Settings can't be edited right now. Please try again shortly.
            </AlertDescription>
        </Alert>
        <TooltipProvider>
            <form className="ps-2" onSubmit={handleSubmit(onSubmitConfig)}>
                <div className="flex items-start gap-4 py-2">
                    <div className="flex items-center gap-2 w-40 pt-1">
                        <Label>Branch</Label>
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <span><InfoIcon /></span>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>The Git branch used for deployment.</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                    <div className="flex-1">
                        <Input
                            {...register("branch", { required: "branch is required" })}
                            type="text"
                            readOnly
                        />
                        {errors.branch && (
                            <p className="text-red-500 text-sm mt-1">
                                {errors.branch.message}
                            </p>
                        )}
                    </div>
                </div>
                <div className="flex items-start gap-4 py-2">
                    <div className="flex items-center gap-2 w-40 pt-1">
                        <Label>Build Directory</Label>
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <span><InfoIcon /></span>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>Path relative to the root of the repository where the build is to be run.</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                    <div className="flex-1">
                        <Input
                            {...register("directory", { required: "directory is required" })}
                            type="text"
                            readOnly
                        />
                        {errors.directory && (
                            <p className="text-red-500 text-sm mt-1">
                                {errors.directory.message}
                            </p>
                        )}
                    </div>
                </div>
                <div className="flex items-start gap-4 py-2">
                    <div className="flex items-center gap-2 w-40 pt-1">
                        <Label>Output Directory</Label>
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <span><InfoIcon /></span>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>Path relative to the root of the repository where the build output is located.</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                    <div className="flex-1">
                        <Input
                            {...register("outDir", { required: "outDir is required" })}
                            type="text"
                            readOnly
                        />
                        {errors.outDir && (
                            <p className="text-red-500 text-sm mt-1">
                                {errors.outDir.message}
                            </p>
                        )}
                    </div>
                </div>

                <div className="flex items-start gap-4 py-2">
                    <div className="flex items-center gap-2 w-40 pt-1">
                        <Label>Build Command</Label>
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <span><InfoIcon /></span>
                            </TooltipTrigger>
                            <TooltipContent>
                                <p>The command that runs your build process (e.g. npm run build).</p>
                            </TooltipContent>
                        </Tooltip>
                    </div>
                    <div className="flex-1">
                        <Input
                            {...register("buildCommand", { required: "buildCommand is required" })}
                            type="text"
                            readOnly
                        />
                        {errors.buildCommand && (
                            <p className="text-red-500 text-sm mt-1">
                                {errors.buildCommand.message}
                            </p>
                        )}
                    </div>
                </div>

                <Button type="submit" className="mt-2" disabled>
                    Save
                </Button>
            </form>
        </TooltipProvider>
    </div>
}

interface IBuildConfig {
    branch: string;
    directory: string;
    buildCommand: string;
    outDir: string;
}

type BuildConfig = {
    branch: string;
    directory: string;
    buildCommand: string;
    outDir: string;
};