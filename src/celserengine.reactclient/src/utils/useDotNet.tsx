import { useEffect, useRef, useState } from "react";
import { jsInteropObj } from "./JsInterop";

export class DotNetObject {
  constructor(
    private objectId: number,
    private dotNetClassName: string,
    private componentId: string
  ) {}
  invokeMethod<T>(methodName: string, ...args: any[]) {
    return jsInteropObj.invokeDotNetMethodAsync<T>(
      this.objectId,
      methodName,
      JSON.stringify(args)
    );
  }

  bindComponentReferences<T>(dotNetObjects: DotNetObject[]) {
    return jsInteropObj.invokeDotNetMethodAsync<T>(
      this.objectId,
      "BindComponentReferences",
      JSON.stringify(
        dotNetObjects.map((obj) => [obj.dotNetClassName, obj.objectId])
      )
    );
  }

  registerComponent(obj: any) {
    jsInteropObj.registerComponent(this.componentId, obj);
    this.invokeMethod("OnComponentRegisteredMethods", this.componentId);
  }

  async detach() {
    jsInteropObj.unregisterComponent(this.componentId);
    await jsInteropObj.detachDotNetObjectAsync(this.objectId);
  }
}

/**
 * A custom React hook that manages the lifecycle of a .NET object associated with a specific component.
 * This hook interacts with a JavaScript interop object to attach and detach .NET objects dynamically.
 *
 * @param componentId - A unique identifier for the component that the .NET object is associated with.
 * @param dotNetClassName - The fully qualified name of the .NET class to instantiate and manage.
 * @returns An instance of `DotNetObject` that allows invoking methods on the .NET object, or `null` if the object is not yet initialized.
 *
 * @remarks
 * - The hook ensures that the .NET object is properly attached when the component is mounted and detached when the component is unmounted.
 * - It uses a `useRef` to store the current `DotNetObject` instance and a `useState` to trigger re-renders when the object is updated.
 * - The hook handles React's strict mode by ensuring that the state is only updated if the component is still mounted.
 *
 * @example
 * ```tsx
 * import { useDotNet } from "./utils/useDotNet";
 *
 * function MyComponent() {
 *   const dotNetObject = useDotNet("myComponentId", "MyNamespace.MyDotNetClass");
 *
 *   useEffect(() => {
 *     if (dotNetObject) {
 *       dotNetObject.invokeMethod("MyMethod", "arg1", "arg2").then((result) => {
 *         console.log(result);
 *       });
 *     }
 *   }, [dotNetObject]);
 *
 *   return <div>My Component</div>;
 * }
 * ```
 */
export function useDotNet(
  componentId: string,
  dotNetClassName: string
): DotNetObject | null {
  const dotNetObjectRef = useRef<DotNetObject | null>(null);
  const [dotNetObject, setDotNetObject] = useState<DotNetObject | null>(null);

  useEffect(() => {
    let isMounted = true;

    const registerDotNetObject = async () => {
      const dotNetObjectId = await jsInteropObj.attachDotNetObjectAsync(
        dotNetClassName,
        componentId
      );

      if (dotNetObjectRef.current != null) {
        await dotNetObjectRef.current.detach();
      }

      dotNetObjectRef.current = new DotNetObject(
        dotNetObjectId,
        dotNetClassName,
        componentId
      );

      // Since we are using strict mode, the component may be mounted and unmounted multiple times.
      // We need to check if the component is still mounted before setting the state.
      if (isMounted) {
        setDotNetObject(dotNetObjectRef.current);
      }
    };

    registerDotNetObject();

    return () => {
      isMounted = false;
      const detachDotNetObject = async () => {
        await dotNetObjectRef.current?.detach();
      };

      detachDotNetObject();
    };
  }, [componentId, dotNetClassName]);

  return dotNetObject;
}
