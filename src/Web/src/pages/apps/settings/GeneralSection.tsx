import { Button } from "flowbite-react";
import { useContext } from "react";
import { AppContext } from "..";
import { useAuth0 } from "@auth0/auth0-react";
import { useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

export default function GeneralSection() {
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();
    const { getAccessTokenSilently } = useAuth0();
    const navigate = useNavigate();
    const handleDeleteClick = async () => {
        if (confirm("Are you sure want to delete this app?")) {
            const accessToken = await getAccessTokenSilently();
            const res = await fetch(`/api/apps/${app.id}`, {
                method: "DELETE",
                headers: { Authorization: `Bearer ${accessToken}` },
            });
            if (res.ok) {
                toast("Deleted app");
                navigate("/apps");
            }
        }
    };

    const handleRenameClick = () => {

    }

    return (
        <section className="space-y-6 rounded-lg border border-slate-200 bg-white p-4">
            {/* Section header */}
            <header>
                <h2 className="text-base font-semibold">
                    General
                </h2>
            </header>

            {/* Rename */}
            <div className="flex items-center justify-between gap-4">
                <div>
                    <h3 className="font-medium">App name</h3>
                    <p className="text-sm text-slate-500">
                        Change the name of this application
                    </p>
                </div>

                <Button
                    type="button"
                    onClick={handleRenameClick}
                >
                    Rename
                </Button>
            </div>

            {/* Divider */}
            <hr className="border-slate-200" />

            {/* Delete */}
            <div className="flex items-center justify-between gap-4">
                <div>
                    <h3 className="font-medium text-red-600">
                        Delete the app
                    </h3>
                    <p className="text-sm text-slate-500">
                        Permanently remove this application and its data
                    </p>
                </div>

                <Button
                    type="button"
                    color="red"
                    onClick={handleDeleteClick}
                >
                    Delete
                </Button>
            </div>
        </section>
    );
}

// type RenameFormInput = { name: string };
// function RenameSection() {
//   const { app } = useContext(AppContext)!;
//   if (!app) throw new Error();
//   const { getAccessTokenSilently } = useAuth0();
//   const schema = yup.object({ name: yup.string().required() });
//   const {
//     register,
//     handleSubmit,
//     formState: { errors },
//   } = useForm<RenameFormInput>({
//     resolver: yupResolver(schema),
//     defaultValues: { name: app.name },
//   });
//   const onSubmit = async (input: RenameFormInput) => {
//     const accessToken = await getAccessTokenSilently();
//     const res = await fetch(`/api/apps/${app.id}/rename`, {
//       method: "PATCH",
//       headers: {
//         "content-type": "application/json",
//         Authorization: `Bearer ${accessToken}`,
//         "If-Match": app.version,
//       },
//       body: JSON.stringify(input),
//     });
//     if (res.ok) {
//       toast("Renamed app");
//     }
//   };

//   return (
//     <>
//       <h3 className="font-semibold">App name</h3>
//       <form onSubmit={handleSubmit(onSubmit)} className="mt-1">
//         <div className="flex gap-1">
//           <div className="flex-1">
//             <TextInput {...register("name")} />
//             {errors.name && <HelperText color="failure">{errors.name.message}</HelperText>}
//           </div>
//           <Button type="submit">Rename</Button>
//         </div>
//       </form>
//     </>
//   );
// }

// function ChangeStateSection() {
//   const { app } = useContext(AppContext)!;
//   if (!app) throw new Error();
//   const { getAccessTokenSilently } = useAuth0();
//   const navigate = useNavigate();
//   const handleChangeStatusClick = async () => {
//     if (
//       app.status === "Active" &&
//       !confirm("Are you sure want to deactivate the app?")
//     ) {
//       return;
//     }
//     const accessToken = await getAccessTokenSilently();
//     const status = app.status === "Active" ? "Inactive" : "Active";
//     const res = await fetch(`/api/apps/${app.id}/status?status=${status}`, {
//       method: "PATCH",
//       headers: { Authorization: `Bearer ${accessToken}` },
//     });
//     if (res.ok) {
//       //TODO: update app status in context
//       app.status = status;
//       toast("Status changed");
//       navigate(".");
//     }
//   };
//   return (
//     <div>
//       <h2 className="font-semibold">Change status</h2>
//       <Button
//         type="button"
//         disabled={app.status === "Blocked"}
//         onClick={handleChangeStatusClick}
//         color={app.status == "Active" ? "red" : "default"}
//       >
//         {app.status === "Active" ? "Deactivate" : "Activate"}
//       </Button>
//     </div>
//   );
// }