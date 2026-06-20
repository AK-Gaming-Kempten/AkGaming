import { auth, isCmsAuthenticationConfigured, signIn, signOut } from "../../../auth";
import "./cms.css";

export default async function CmsPage() {
    if (!isCmsAuthenticationConfigured())
        return <ConfigurationRequiredPanel />;

    const session = await auth();

    if (session === null)
        return <SignInPanel />;

    if (!session.roles.some(role => role.localeCompare("Admin", undefined, { sensitivity: "accent" }) === 0))
        return <AccessDeniedPanel email={session.user?.email} roles={session.roles} />;

    return <CmsOverview email={session.user?.email} />;
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

function AccessDeniedPanel({ email, roles }: { email?: string | null; roles: string[] }) {
    async function signOutFromCms() {
        "use server";
        await signOut({ redirectTo: "/cms" });
    }

    return (
        <main className="cms-page">
            <section className="cms-panel">
                <p className="cms-eyebrow">Access denied</p>
                <h1>CMS access is restricted</h1>
                <p>{email ?? "This account"} does not have the AK Gaming Admin role.</p>
                <p>Roles received from Identity: {roles.length === 0 ? "none" : roles.join(", ")}.</p>
                <form action={signOutFromCms}>
                    <button type="submit" className="cms-secondary-button">Sign out</button>
                </form>
            </section>
        </main>
    );
}

function CmsOverview({ email }: { email?: string | null }) {
    async function signOutFromCms() {
        "use server";
        await signOut({ redirectTo: "/cms" });
    }

    return (
        <main className="cms-page">
            <section className="cms-panel cms-panel-wide">
                <div className="cms-header">
                    <div>
                        <p className="cms-eyebrow">AK Gaming CMS</p>
                        <h1>Content administration</h1>
                        <p>Signed in as {email ?? "Administrator"}.</p>
                    </div>
                    <form action={signOutFromCms}>
                        <button type="submit" className="cms-secondary-button">Sign out</button>
                    </form>
                </div>
                <div className="cms-card-grid">
                    <article>
                        <h2>Posts and events</h2>
                        <p>Draft, preview, and publish MDX content.</p>
                    </article>
                    <article>
                        <h2>Media library</h2>
                        <p>Upload and organize images used by content.</p>
                    </article>
                    <article>
                        <h2>Website data</h2>
                        <p>Manage highlights, esports data, navigation, and future page content.</p>
                    </article>
                </div>
            </section>
        </main>
    );
}
