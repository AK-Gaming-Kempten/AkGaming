import { NextResponse } from "next/server";
import { isCmsAdministrator } from "../../../../../content/cmsAuthorization";
import { deleteManagedTeam } from "../../../../../content/teamStore";

type TeamRouteContext = { params: Promise<{ teamId: string }> };

export async function DELETE(_request: Request, context: TeamRouteContext) {
    if (!await isCmsAdministrator())
        return NextResponse.json({ message: "Forbidden." }, { status: 403 });

    try {
        const { teamId } = await context.params;
        await deleteManagedTeam(teamId);
        return new NextResponse(null, { status: 204 });
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "The team could not be deleted." }, { status: 400 });
    }
}
