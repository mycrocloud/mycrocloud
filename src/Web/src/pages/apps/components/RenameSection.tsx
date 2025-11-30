import { useForm } from "react-hook-form";

interface RenameSectionProps {
  defaultName: string;
  onRename: (name: string) => void;
}

export default function RenameSection({ defaultName, onRename }: RenameSectionProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<{ name: string }>({
    defaultValues: { name: defaultName },
  });

  const onSubmit = (data: { name: string }) => {
    onRename(data.name);
  };

  return (
    <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6 mt-2">
      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
        App Name
      </h3>
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col sm:flex-row gap-2">
        <div className="flex-1">
          <input
            type="text"
            {...register("name", { required: "Name is required" })}
            className={`block w-full rounded-md border border-gray-300 dark:border-gray-600 px-3 py-2 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:border-primary focus:ring-1 focus:ring-primary dark:focus:border-primary dark:focus:ring-primary`}
            autoComplete="off"
          />
          {errors.name && (
            <p className="mt-1 text-sm text-red-500">{errors.name.message}</p>
          )}
        </div>
        <button
          type="submit"
          className="bg-primary text-white px-4 py-2 rounded-md hover:bg-primary-dark transition-colors sm:self-end"
        >
          Rename
        </button>
      </form>
    </div>
  );
}

// type RenameFormInput = { name: string };
// function RenameSection() {
//   const { app } = useApp();
//   if (!app) return <Spinner aria-label="Loading..." />

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
//         <div className="flex">
//           <div>
//             <input
//               type="text"
//               {...register("name")}
//               className="block border px-2 py-0.5"
//               autoComplete="off"
//             />
//             {errors.name && (
//               <span className="text-red-500">{errors.name.message}</span>
//             )}
//           </div>
//           <div className="relative ms-1">
//             <button
//               type="submit"
//               className="absolute top-0 my-auto bg-primary px-2 py-0.5 text-white"
//             >
//               Rename
//             </button>
//           </div>
//         </div>
//       </form>
//     </>
//   );
// }
