"use client";

import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { LuCircleAlert, LuCircleCheck, LuInfo, LuX } from "react-icons/lu";

type CmsToastVariant = "success" | "error" | "info";

type CmsToast = {
    id: number;
    message: string;
    variant: CmsToastVariant;
};

type CmsToastContextValue = {
    showToast: (message: string, variant?: CmsToastVariant) => void;
};

const CmsToastContext = createContext<CmsToastContextValue | null>(null);

export function CmsToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<CmsToast[]>([]);
    const nextId = useRef(0);

    const dismissToast = useCallback((id: number) => {
        setToasts(current => current.filter(toast => toast.id !== id));
    }, []);

    const showToast = useCallback((message: string, variant: CmsToastVariant = "success") => {
        const id = nextId.current;
        nextId.current += 1;
        setToasts(current => [...current, { id, message, variant }]);
    }, []);

    return (
        <CmsToastContext.Provider value={{ showToast }}>
            {children}
            <div className="cms-toast-stack" aria-live="polite" aria-relevant="additions">
                {toasts.map(toast => <CmsToastNotification key={toast.id} toast={toast} onDismiss={dismissToast} />)}
            </div>
        </CmsToastContext.Provider>
    );
}

export function useCmsToast(): CmsToastContextValue {
    const context = useContext(CmsToastContext);
    if (context === null)
        throw new Error("useCmsToast must be used within CmsToastProvider.");

    return context;
}

function CmsToastNotification({ toast, onDismiss }: { toast: CmsToast; onDismiss: (id: number) => void }) {
    useEffect(() => {
        const timeout = window.setTimeout(() => onDismiss(toast.id), 5000);
        return () => window.clearTimeout(timeout);
    }, [onDismiss, toast.id]);

    const title = toast.variant === "error" ? "Action failed" : toast.variant === "info" ? "Information" : "Saved";
    const Icon = toast.variant === "error" ? LuCircleAlert : toast.variant === "info" ? LuInfo : LuCircleCheck;

    return (
        <aside className={`cms-toast cms-toast-${toast.variant}`} role={toast.variant === "error" ? "alert" : "status"}>
            <header className="cms-toast-header">
                <div className="cms-toast-brand">
                    <span className="cms-toast-icon"><Icon /></span>
                    <div><p>AK Gaming CMS</p><h2>{title}</h2></div>
                </div>
                <button type="button" onClick={() => onDismiss(toast.id)} aria-label="Dismiss notification" title="Dismiss notification"><LuX /></button>
            </header>
            <p className="cms-toast-body">{toast.message}</p>
        </aside>
    );
}
