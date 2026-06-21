import { NextResponse } from "next/server";
import { isCmsAdministrator } from "../../../../../content/cmsAuthorization";
import { listManagedGames, saveManagedGames, type ManagedEsportsGame } from "../../../../../content/esportsCatalogStore";

export async function GET() {
    if (!await isCmsAdministrator()) return NextResponse.json({ message: "Forbidden." }, { status: 403 });
    return NextResponse.json(await listManagedGames());
}

export async function PUT(request: Request) {
    if (!await isCmsAdministrator()) return NextResponse.json({ message: "Forbidden." }, { status: 403 });
    try {
        return NextResponse.json(await saveManagedGames(await request.json() as ManagedEsportsGame[]));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "Games could not be saved." }, { status: 400 });
    }
}
