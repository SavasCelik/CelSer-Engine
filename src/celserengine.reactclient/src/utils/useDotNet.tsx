import { useEffect, useRef, useState } from "react";
import { jsInteropObj } from './JsInterop2_1';

class DotNetObject {
    constructor(private objectId: number) { }
    invokeMethod<T>(methodName: string, ...args: any[]) {
        return jsInteropObj.invokeDotNetMethodAsync<T>(this.objectId, methodName, JSON.stringify(args));
    }

    async detach() {
        await jsInteropObj.DetachDotNetObjectAsync(this.objectId);
    }
}

export function useDotNet(componentId: string, dotNetClassName: string): DotNetObject | null {
    const dotNetObjectRef = useRef<DotNetObject | null>(null);
    const [dotNetObject, setDotNetObject] = useState<DotNetObject | null>(null);

    useEffect(() => {
        const registerDotNetObject = async () => {
            const dotNetObjectId = await jsInteropObj.AttachDotNetObjectAsync(dotNetClassName, componentId);

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
            }

            detachDotNetObject();
        };
    }, [componentId, dotNetClassName]);

    return dotNetObject;
}
