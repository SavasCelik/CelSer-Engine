import { useEffect, useRef, useState } from "react";
import { jsInteropObj } from "./JsInterop";

class DotNetObject {
  constructor(private objectId: number) {}
  invokeMethod<T>(methodName: string, ...args: any[]) {
    return jsInteropObj.invokeDotNetMethodAsync<T>(
      this.objectId,
      methodName,
      JSON.stringify(args)
    );
  }

  async detach() {
    await jsInteropObj.detachDotNetObjectAsync(this.objectId);
  }
}

export function useDotNet(
  componentId: string,
  dotNetClassName: string
): DotNetObject | null {
  const dotNetObjectRef = useRef<DotNetObject | null>(null);
  const [dotNetObject, setDotNetObject] = useState<DotNetObject | null>(null);

  useEffect(() => {
    const registerDotNetObject = async () => {
      const dotNetObjectId = await jsInteropObj.attachDotNetObjectAsync(
        dotNetClassName,
        componentId
      );

      if (dotNetObjectRef.current != null) {
        await dotNetObjectRef.current.detach();
      }

      dotNetObjectRef.current = new DotNetObject(dotNetObjectId);
      setDotNetObject(dotNetObjectRef.current);
    };

    registerDotNetObject();

    return () => {
      const detachDotNetObject = async () => {
        await dotNetObjectRef.current?.detach();
      };

      detachDotNetObject();
    };
  }, [componentId, dotNetClassName]);

  return dotNetObject;
}
