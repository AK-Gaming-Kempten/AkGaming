import NextAuth from "next-auth";
import { customFetch } from "next-auth";
import type { OAuthConfig } from "@auth/core/providers/oauth";
import type { Profile } from "@auth/core/types";
import { request as httpsRequest } from "node:https";
import { decodeCmsCapabilities, encodeCmsCapabilities } from "./src/content/cmsPermissions";

const akGamingProvider: OAuthConfig<Profile> = {
    id: "akgaming",
    name: "AK Gaming Identity",
    type: "oidc",
    issuer: process.env.AUTH_AKG_ISSUER,
    clientId: process.env.AUTH_AKG_CLIENT_ID,
    clientSecret: process.env.AUTH_AKG_CLIENT_SECRET,
    idToken: false,
    authorization: {
        params: {
            scope: "openid profile email roles",
        },
    },
    [customFetch]: shouldTrustLocalDevelopmentIdentity()
        ? fetchWithLocalDevelopmentCertificate
        : undefined,
    profile(profile) {
        return {
            id: profile.sub ?? "",
            name: profile.name ?? profile.preferred_username ?? profile.email ?? "AK Gaming user",
            email: profile.email,
        };
    },
};

export const { handlers, auth, signIn, signOut } = NextAuth({
    providers: [akGamingProvider],
    trustHost: true,
    callbacks: {
        jwt({ token, profile, account }) {
            if (profile !== undefined || account?.id_token !== undefined) {
                const profilePermissions = profile === undefined ? [] : getPermissions(profile);
                const idTokenPermissions = getPermissionsFromIdToken(account?.id_token);
                const permissions = profilePermissions.length > 0 ? profilePermissions : idTokenPermissions;
                token.cmsCapabilities = encodeCmsCapabilities(permissions);
            }

            delete token.roles;
            delete token.permissions;

            return token;
        },
        session({ session, token }) {
            session.permissions = decodeCmsCapabilities(typeof token.cmsCapabilities === "string" ? token.cmsCapabilities : undefined);
            return session;
        },
    },
});

export function isCmsAuthenticationConfigured(): boolean {
    return [
        process.env.AUTH_AKG_ISSUER,
        process.env.AUTH_AKG_CLIENT_ID,
        process.env.AUTH_AKG_CLIENT_SECRET,
        process.env.AUTH_SECRET,
    ].every(value => !isBlank(value));
}

function getPermissions(source: Record<string, unknown>): string[] {
    const permissionValue = source.permission ?? source.permissions;

    if (typeof permissionValue === "string")
        return [permissionValue];

    if (Array.isArray(permissionValue))
        return permissionValue.filter((value): value is string => typeof value === "string");

    return [];
}

function getPermissionsFromIdToken(idToken: string | undefined): string[] {
    return getClaimsFromIdToken(idToken, getPermissions);
}

function getClaimsFromIdToken(idToken: string | undefined, getClaims: (source: Record<string, unknown>) => string[]): string[] {
    if (idToken === undefined)
        return [];

    const payload = idToken.split(".")[1];
    if (payload === undefined)
        return [];

    try {
        const value = JSON.parse(Buffer.from(payload, "base64url").toString("utf8"));
        return isRecord(value) ? getClaims(value) : [];
    }
    catch {
        return [];
    }
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
}

function isBlank(value: string | undefined): boolean {
    return value === undefined || value.trim().length === 0 || value.startsWith("replace-");
}

function shouldTrustLocalDevelopmentIdentity(): boolean {
    if (process.env.NODE_ENV !== "development")
        return false;

    const issuer = process.env.AUTH_AKG_ISSUER;
    if (issuer === undefined)
        return false;

    try {
        const host = new URL(issuer).hostname;
        return host === "localhost" || host === "127.0.0.1" || host === "::1";
    }
    catch {
        return false;
    }
}

function fetchWithLocalDevelopmentCertificate(
    input: Parameters<typeof fetch>[0],
    init?: Parameters<typeof fetch>[1],
): Promise<Response> {
    const url = typeof input === "string" || input instanceof URL ? input : input.url;

    return new Promise((resolve, reject) => {
        const request = httpsRequest(url, {
            method: init?.method ?? "GET",
            headers: toNodeHeaders(init?.headers),
            rejectUnauthorized: false,
        }, response => {
            const chunks: Buffer[] = [];
            response.on("data", chunk => chunks.push(Buffer.from(chunk)));
            response.on("error", reject);
            response.on("end", () => {
                resolve(new Response(Buffer.concat(chunks), {
                    status: response.statusCode ?? 500,
                    statusText: response.statusMessage,
                    headers: toResponseHeaders(response.headers),
                }));
            });
        });

        request.on("error", reject);
        writeRequestBody(request, init?.body);
    });
}

function toNodeHeaders(headers?: HeadersInit): Record<string, string> {
    const result: Record<string, string> = {};
    new Headers(headers).forEach((value, key) => {
        result[key] = value;
    });
    return result;
}

function toResponseHeaders(headers: NodeJS.Dict<string | string[]>): Headers {
    const result = new Headers();
    for (const [key, value] of Object.entries(headers)) {
        if (value === undefined)
            continue;

        if (Array.isArray(value)) {
            for (const item of value)
                result.append(key, item);
            continue;
        }

        result.set(key, value);
    }
    return result;
}

function writeRequestBody(request: ReturnType<typeof httpsRequest>, body: BodyInit | null | undefined): void {
    if (body === undefined || body === null) {
        request.end();
        return;
    }

    if (body instanceof URLSearchParams) {
        request.end(body.toString());
        return;
    }

    if (typeof body === "string" || body instanceof Uint8Array) {
        request.end(body);
        return;
    }

    request.destroy(new Error("The local identity development fetch does not support streaming request bodies."));
}
