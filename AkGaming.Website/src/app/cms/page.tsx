import { auth, isCmsAuthenticationConfigured, signIn, signOut } from "../../../auth";
import "./cms.css";
import CmsPostsEditor from "./CmsPostsEditor";
import { canAccessCms } from "../../content/cmsAuthorization";
import CmsAccessThemeControls from "./CmsAccessThemeControls";

export default async function CmsPage() {
    if (!isCmsAuthenticationConfigured())
        return <CmsPageShell><ConfigurationRequiredPanel /></CmsPageShell>;

    const session = await auth();

    if (session === null)
        return <CmsPageShell><SignInPanel /></CmsPageShell>;

    if (!canAccessCms(session.permissions))
        return <CmsPageShell><AccessDeniedPanel email={session.user?.email} /></CmsPageShell>;

    return <CmsPageShell><CmsOverview email={session.user?.email} permissions={session.permissions} /></CmsPageShell>;
}

function CmsPageShell({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <>
            <main className="cms-mobile-warning">
                <section className="cms-mobile-warning-panel">
                    <p className="cms-eyebrow">Desktop required</p>
                    <h1>Use the CMS on a larger screen</h1>
                    <p>The content management interface is designed for desktop and laptop displays. Please continue on a screen at least 900 pixels wide.</p>
                    <a href="/">Return to website</a>
                </section>
            </main>
            <div className="cms-desktop-content">{children}</div>
        </>
    );
}

function ConfigurationRequiredPanel() {
    return (
        <main className="cms-page">
            <CmsAccessThemeControls />
            <section className="cms-panel">
                <p className="cms-eyebrow">CMS setup</p>
                <h1>Identity configuration required</h1>
                <p>
                    Copy <code>.env.example</code> to <code>.env.local</code> and configure the
                    CMS OIDC client before signing in.
                </p>
            </section>
        </main>
    );
}

function SignInPanel() {
    async function signInToCms() {
        "use server";
        await signIn("akgaming", { redirectTo: "/cms" });
    }

    return (
        <main className="cms-page">
            <CmsAccessThemeControls />
            <section className="cms-panel">
                <p className="cms-eyebrow">AK Gaming</p>
                <h1>Website content management</h1>
                <p>Sign in with your AK Gaming account to manage website content.</p>
                <form action={signInToCms}>
                    <button type="submit">Sign in</button>
                </form>
            </section>
        </main>
    );
}

function AccessDeniedPanel({ email }: { email?: string | null }) {
    async function signOutFromCms() {
        "use server";
        await signOut({ redirectTo: "/cms" });
    }

    return (
        <main className="cms-page">
            <CmsAccessThemeControls />
            <section className="cms-panel">
                <p className="cms-eyebrow">Access denied</p>
                <h1>CMS access is restricted</h1>
                <p>{email ?? "This account"} does not have a website CMS permission.</p>
                <form action={signOutFromCms}>
                    <button type="submit" className="cms-secondary-button">Sign out</button>
                </form>
            </section>
        </main>
    );
}

function CmsOverview({ email, permissions }: { email?: string | null; permissions: string[] }) {
    async function signOutFromCms() {
        "use server";
        await signOut({ redirectTo: "/cms" });
    }

    return (
        <main className="cms-page cms-fullscreen">
            <CmsPostsEditor email={email} permissions={permissions} signOutAction={signOutFromCms} />
        </main>
    );
}
