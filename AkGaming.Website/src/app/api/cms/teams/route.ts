import { NextResponse } from "next/server";
import { isCmsAdministrator } from "../../../../content/cmsAuthorization";
import { listManagedTeams, saveManagedTeam, type ManagedEsportsTeam } from "../../../../content/teamStore";

export async function GET() {
    if (!await isCmsAdministrator())
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    return NextResponse.json(await listManagedTeams());
}

export async function POST(request: Request) {
    if (!await isCmsAdministrator())
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { team, previousId } = await request.json() as { team: ManagedEsportsTeam; previousId?: string };
        return NextResponse.json(await saveManagedTeam(team, previousId), { status: 201 });
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The team could not be saved." }, { status: 400 });
    }
}
