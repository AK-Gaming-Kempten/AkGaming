import "server-only";

import { auth } from "../../auth";
import { CmsPermissions } from "./cmsPermissions";

export { CmsPermissions };

export async function hasCmsPermission(permission: string): Promise<boolean> {
    const session = await auth();
    return session?.permissions.includes(permission) ?? false;
}

export function canAccessCms(permissions: readonly string[]): boolean {
    return [
        CmsPermissions.postsManage,
        CmsPermissions.mediaManage,
        CmsPermissions.highlightsManage,
        CmsPermissions.esportsManage,
    ].some(permission => permissions.includes(permission));
}
