import { useEffect, useState } from "react";
import IScheme from "./IScheme";
import { useApp } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { useFieldArray, useForm } from "react-hook-form";
import { DndContext, DragEndEvent } from "@dnd-kit/core";
import { SortableContext, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { ArrowLeftIcon, ArrowRightIcon } from "@heroicons/react/24/solid";
import { Alert, Spinner } from "flowbite-react";

type Inputs = {
  schemes: IScheme[];
};
export default function AuthenticationSettings() {
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const { getAccessTokenSilently } = useAuth0();
  const [schemes, setSchemes] = useState<IScheme[]>([]);

  const {
    handleSubmit,
    control,
    setValue,
    formState: { errors },
  } = useForm<Inputs>();

  const { fields, append, swap, remove } = useFieldArray({
    control,
    name: "schemes",
    keyName: "fieldId",
  });

  const availableSchemes = schemes.filter(
    (s) => fields.findIndex((sf) => sf.id == s.id) === -1,
  );

  const onSubmit = async (data: Inputs) => {
    const schemeIds = data.schemes.map((s) => s.id);
    const accessToken = await getAccessTokenSilently();
    await fetch(`/api/apps/${app.id}/authentications/schemes/settings`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(schemeIds),
    });
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;
    if (active.id !== over.id) {
      const activeIndex = fields.findIndex((f) => f.id === active.id);
      const overIndex = fields.findIndex((f) => f.id === over.id);
      swap(activeIndex, overIndex);
    }
  };

  const handleDisableClick = (id: number) => {
    const index = fields.findIndex((f) => f.id === id);
    remove(index);
  };

  const handleEnableClick = (id: number) => {
    const scheme = schemes.find((s) => s.id === id)!;
    append(scheme);
  };

  useEffect(() => {
    const getSchemes = async () => {
      const accessToken = await getAccessTokenSilently();
      const schemes = (await (
        await fetch(`/api/apps/${app.id}/authentications/schemes`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        })
      ).json()) as IScheme[];
      setSchemes(schemes);
      setValue(
        "schemes",
        schemes.filter((s) => s.enabled),
      );
    };
    getSchemes();
  }, []);
  return (
    <form className="p-2" onSubmit={handleSubmit(onSubmit)}>
      <h1 className="font-bold">Authentication Settings</h1>
      <Alert color="info" className="mt-2">
        <span className="font-medium">
          Drag from the right to the left to enable a scheme. Drag and drop to
          reorder the enabled schemes.
        </span>
      </Alert>
      <div className="mt-3 flex w-full">
        <div className="flex-1 border p-2">
          <h2 className="text-center">Enabled Schemes</h2>
          <DndContext onDragEnd={handleDragEnd}>
            <SortableContext items={fields}>
              <div>
                {fields.length ? (
                  fields.map((scheme) => (
                    <SortableItem
                      key={scheme.id}
                      scheme={scheme}
                      onDisableClick={handleDisableClick}
                    />
                  ))
                ) : (
                  <div>
                    <span className="text-gray-400">No schemes enabled.</span>
                  </div>
                )}
              </div>
            </SortableContext>
          </DndContext>
        </div>
        <div className="flex-1 border p-2">
          <h2 className="text-center">Available Schemes</h2>
          <div>
            {availableSchemes.length ? (
              availableSchemes.map((s) => (
                <AvailableItem scheme={s} onEnableClick={handleEnableClick} />
              ))
            ) : (
              <div>
                <span className="text-gray-400">No schemes available.</span>
              </div>
            )}
          </div>
        </div>
        {errors.schemes && (
          <span className="text-red-500">{errors.schemes.message}</span>
        )}
      </div>
      <div className="mt-2">
        <button
          type="submit"
          className="border border-transparent bg-primary px-2 py-1 text-center font-medium text-white focus:z-10 focus:outline-none focus:ring-2 focus:ring-cyan-300 enabled:hover:bg-cyan-800"
        >
          Save
        </button>
      </div>
    </form>
  );
}

function SortableItem({
  scheme,
  onDisableClick,
}: {
  scheme: IScheme;
  onDisableClick: (id: number) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition } =
    useSortable({ id: scheme.id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };
  return (
    <div style={style} className="flex py-1">
      <div
        ref={setNodeRef}
        {...attributes}
        {...listeners}
        className="flex-auto border p-1"
      >
        {scheme.name}
      </div>
      <button
        type="button"
        onClick={() => onDisableClick(scheme.id)}
        className="ms-auto p-1"
      >
        <ArrowRightIcon className="h-4 w-4 text-blue-500" />
      </button>
    </div>
  );
}

function AvailableItem({
  scheme,
  onEnableClick,
}: {
  scheme: IScheme;
  onEnableClick: (id: number) => void;
}) {
  return (
    <div className="flex py-1">
      <button
        type="button"
        onClick={() => onEnableClick(scheme.id)}
        className="ms-auto p-1"
      >
        <ArrowLeftIcon className="h-4 w-4 text-blue-500" />
      </button>
      <div className="flex-auto border p-1">{scheme.name}</div>
    </div>
  );
}
