import { NextResponse } from "next/server";
import { CmsPermissions, hasCmsPermission } from "../../../../../content/cmsAuthorization";
import { listManagedLeagues, saveManagedLeagues, type ManagedEsportsLeague } from "../../../../../content/esportsCatalogStore";

export async function GET() {
    if (!await hasCmsPermission(CmsPermissions.esportsManage)) return NextResponse.json({ message: "Forbidden." }, { status: 403 });
    return NextResponse.json(await listManagedLeagues());
}

export async function PUT(request: Request) {
    if (!await hasCmsPermission(CmsPermissions.esportsManage)) return NextResponse.json({ message: "Forbidden." }, { status: 403 });
    try {
        return NextResponse.json(await saveManagedLeagues(await request.json() as ManagedEsportsLeague[]));
    }
    catch (error) {
        return NextResponse.json({ message: error instanceof Error ? error.message : "Leagues could not be saved." }, { status: 400 });
    }
}
