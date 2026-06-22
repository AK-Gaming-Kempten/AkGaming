export const CmsPermissions = {
    postsManage: "website.cms.posts.manage",
    postsPublish: "website.cms.posts.publish",
    mediaManage: "website.cms.media.manage",
    highlightsManage: "website.cms.highlights.manage",
    esportsManage: "website.cms.esports.manage",
} as const;

const orderedPermissions = Object.values(CmsPermissions);

export function encodeCmsCapabilities(permissions: readonly string[]): string {
    const bits = orderedPermissions.reduce((value, permission, index) =>
        permissions.includes(permission) ? value | (1 << index) : value, 0);
    return bits.toString(36);
}

export function decodeCmsCapabilities(capabilities: string | undefined): string[] {
    const bits = capabilities === undefined ? 0 : Number.parseInt(capabilities, 36);
    if (!Number.isFinite(bits) || bits < 0)
        return [];

    return orderedPermissions.filter((_, index) => (bits & (1 << index)) !== 0);
}
