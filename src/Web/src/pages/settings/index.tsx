import Connections from "./Connections";
import Tokens from "./Tokens";

export default function Settings() {
  return (
    <div className="w-10/12 mx-auto mt-4">
      <h1 className="font-bold">Settings</h1>
      <Connections />
      <Tokens />
    </div>
  );
}
