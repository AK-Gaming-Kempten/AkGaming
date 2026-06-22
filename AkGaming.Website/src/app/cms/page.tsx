import { auth, isCmsAuthenticationConfigured, signIn, signOut } from "../../../auth";
import "./cms.css";
import CmsPostsEditor from "./CmsPostsEditor";
import { canAccessCms } from "../../content/cmsAuthorization";

export default async function CmsPage() {
    if (!isCmsAuthenticationConfigured())
        return <ConfigurationRequiredPanel />;

    const session = await auth();

    if (session === null)
        return <SignInPanel />;

    if (!canAccessCms(session.permissions))
        return <AccessDeniedPanel email={session.user?.email} />;

    return <CmsOverview email={session.user?.email} permissions={session.permissions} />;
}

function ConfigurationRequiredPanel() {
    return (
        <main className="cms-page">
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
