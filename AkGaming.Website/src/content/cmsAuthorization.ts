import "server-only";

import { auth } from "../../auth";

export async function isCmsAdministrator(): Promise<boolean> {
    const session = await auth();
    return session?.roles.some(role => role.localeCompare("Admin", undefined, { sensitivity: "accent" }) === 0) ?? false;
}
