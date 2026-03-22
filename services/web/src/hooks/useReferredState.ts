import { useRef, useState } from "react";

export const useReferredState = <T>(
  // @ts-expect-error -- generic default requires undefined as valid T
  initialValue: T = undefined,
): [T, React.MutableRefObject<T>, React.Dispatch<T>] => {
  const [state, setState] = useState<T>(initialValue);
  const reference = useRef<T>(state);

  // @ts-expect-error -- generic default requires undefined as valid T
  const setReferredState = (value) => {
    reference.current = value;
    setState(value);
  };

  return [state, reference, setReferredState];
};
