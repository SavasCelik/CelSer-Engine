/* eslint-disable @typescript-eslint/no-explicit-any */
interface PendingAsyncCall<T> {
    resolve: (value?: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}

class JsInterop {
    private componentRegistry: Map<string, any> = new Map<string, any>();
    private readonly _pendingAsyncCalls: { [id: number]: PendingAsyncCall<any> } = {};
    private _requestId = 0;

    attachDotNetObjectAsync(dotNetTypeName: string, componentId: string): Promise<number> {
        return this.invokeDotNetMethodAsync<number>(null, "AttachDotNetObject", JSON.stringify([ dotNetTypeName, componentId ]));
    }

    detachDotNetObjectAsync(dotNetRefId: number): Promise<unknown> {
        return this.invokeDotNetMethodAsync(null, "DetachDotNetObject", JSON.stringify(dotNetRefId));
    }

    invokeDotNetMethodAsync<T>(dotNetObjectId?: number | null, methodName?: string, methodArguments?: any): Promise<T> {
        const asyncCallId = this._requestId++;
        const resultPromise = new Promise<T>((resolve, reject) => {
            this._pendingAsyncCalls[asyncCallId] = { resolve, reject };
        });
        const message = JSON.stringify({
            asyncCallId,
            dotNetObjectId,
            methodName,
            methodArguments,
        });
        (window as any).chrome.webview.postMessage(message);

        return resultPromise;
    }

    handleResponse(event: MessageEvent) {
        const response = event.data

        if (response.asyncCallId !== undefined && Object.prototype.hasOwnProperty.call(this._pendingAsyncCalls, response.asyncCallId)) {
            const asyncCall = this._pendingAsyncCalls[response.asyncCallId];
            delete this._pendingAsyncCalls[response.asyncCallId];
            asyncCall.resolve(JSON.parse(response.reposeJson));
        }
    }

    registerComponent(componentId: string, obj: any): void {
        const existingComponent = this.componentRegistry.get(componentId);

        if (existingComponent) {
            throw new Error(`Component with ID ${componentId} is already registered.`);
        }

        this.componentRegistry.set(componentId, obj);
    }

    invokeComponentMethod(componentId: string, methodName: string, argsAsJson: any): string {
        const obj = this.componentRegistry.get(componentId);
        if (!obj) {
            throw new Error(`Component with ID ${componentId} is not registered.`);
        }
        const method = obj[methodName];
        if (typeof method === 'function') {
            //const args = JSON.parse(argsAsJson);
            return JSON.stringify(method(...argsAsJson));
        } else {
            throw new Error(`Method ${methodName} is not defined on component with ID ${componentId}.`);
        }
    }

    unregisterComponent(componentId: string): boolean {
        return this.componentRegistry.delete(componentId);
    }
}

// Expose the JsInterop instance globally

export const jsInteropObj = new JsInterop();
(window as any).chrome.webview.addEventListener("message", (event: any) => jsInteropObj.handleResponse(event));

(window as any)["jsInterop"] = jsInteropObj;

