import type { ReactNode } from "react";
import type { IconType } from "react-icons";
import {
    FaBars,
    FaColumns,
    FaExternalLinkAlt,
    FaImage,
    FaInfoCircle,
    FaParagraph,
    FaPenFancy,
    FaTable,
    FaTag,
    FaThLarge,
    FaThList,
    FaVideo,
} from "react-icons/fa";
import sponsorLogo from "../../../public/assets/soloplan_mit_text.png";
import SmartLink from "./SmartLink";
import ButtonLink from "./ButtonLink";
import ButtonRow from "./ButtonRow";
import Lead from "./Lead";
import Callout from "./Callout";
import CardGrid from "./CardGrid";
import Card from "./Card";
import Columns from "./Columns";
import Stack from "./Stack";
import Embed from "./Embed";
import LinkedImage from "./LinkedImage";
import Table from "./Table";
import Text from "./Text";

export type MdxComponentDoc = {
    name: string;
    slug: string;
    category: string;
    icon: IconType;
    syntax: string;
    description: string;
    props: {
        name: string;
        type: string;
        required?: boolean;
        defaultValue?: string;
        description: string;
    }[];
    example: string;
    preview: ReactNode;
};

export const mdxComponents = {
    a: SmartLink,
    ButtonLink,
    ButtonRow,
    Lead,
    Callout,
    CardGrid,
    Card,
    Columns,
    Stack,
    Embed,
    LinkedImage,
    Table,
    Text,
};

export const mdxComponentDocs: MdxComponentDoc[] = [
    {
        name: "Link",
        slug: "link",
        category: "Text and Actions",
        icon: FaExternalLinkAlt,
        syntax: "[Label](https://example.com) or [Intern](/events)",
        description: "Markdown links are automatically rendered through the smart link component.",
        props: [
            {
                name: "href",
                type: "string",
                required: true,
                description: "Target URL from the Markdown link. Internal links starting with / use router navigation.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Visible link label between the Markdown brackets.",
            },
        ],
        example: "[Eventseite](/events)\n[Discord](https://discord.gg/5J5uJKJAhT)",
        preview: (
            <Stack>
                <p><SmartLink href="/events">Eventseite</SmartLink></p>
                <p><SmartLink href="https://discord.gg/5J5uJKJAhT">Discord</SmartLink></p>
            </Stack>
        ),
    },
    {
        name: "Lead",
        slug: "lead",
        category: "Text and Actions",
        icon: FaParagraph,
        syntax: "<Lead>...</Lead>",
        description: "Creates a larger intro paragraph below the title or hero area.",
        props: [
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Lead paragraph content rendered with stronger visual emphasis.",
            },
        ],
        example: "<Lead>Ein kurzer Einleitungstext mit etwas mehr Gewicht.</Lead>",
        preview: <Lead>Ein kurzer Einleitungstext mit etwas mehr Gewicht.</Lead>,
    },
    {
        name: "ButtonLink",
        slug: "button-link",
        category: "Text and Actions",
        icon: FaExternalLinkAlt,
        syntax: "<ButtonLink href=\"...\" variant=\"primary|secondary\">...</ButtonLink>",
        description: "Renders a CTA button for internal or external links.",
        props: [
            {
                name: "href",
                type: "string",
                required: true,
                description: "Internal or external destination for the button.",
            },
            {
                name: "variant",
                type: "\"primary\" | \"secondary\"",
                defaultValue: "\"primary\"",
                description: "Visual style of the button.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Button label content.",
            },
        ],
        example: "<ButtonLink href=\"/events\">Events</ButtonLink>",
        preview: (
            <ButtonRow>
                <ButtonLink href="/events">Events</ButtonLink>
                <ButtonLink href="/mitgliedschaft" variant="secondary">Mitglied werden</ButtonLink>
            </ButtonRow>
        ),
    },
    {
        name: "ButtonRow",
        slug: "button-row",
        category: "Text and Actions",
        icon: FaBars,
        syntax: "<ButtonRow>...</ButtonRow>",
        description: "Places multiple buttons in one horizontal row with wrapping.",
        props: [
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Usually a set of ButtonLink components laid out in one row with wrapping.",
            },
        ],
        example: "<ButtonRow>\n  <ButtonLink href=\"/events\">Events</ButtonLink>\n  <ButtonLink href=\"/esports\" variant=\"secondary\">Esports</ButtonLink>\n</ButtonRow>",
        preview: (
            <ButtonRow>
                <ButtonLink href="/events">Events</ButtonLink>
                <ButtonLink href="/esports" variant="secondary">Esports</ButtonLink>
            </ButtonRow>
        ),
    },
    {
        name: "Callout",
        slug: "callout",
        category: "Text and Actions",
        icon: FaInfoCircle,
        syntax: "<Callout title=\"...\" tone=\"neutral|accent|success|warning\">...</Callout>",
        description: "Highlights important information in a framed box.",
        props: [
            {
                name: "title",
                type: "string",
                description: "Optional heading shown above the callout body.",
            },
            {
                name: "tone",
                type: "\"neutral\" | \"accent\" | \"success\" | \"warning\"",
                defaultValue: "\"neutral\"",
                description: "Visual tone used to communicate emphasis or intent.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Main content inside the callout.",
            },
        ],
        example: "<Callout title=\"Wichtig\" tone=\"accent\">Bitte melde dich vorher an.</Callout>",
        preview: (
            <Callout title="Wichtig" tone="accent">
                Bitte melde dich vorher an.
            </Callout>
        ),
    },
    {
        name: "Card",
        slug: "card",
        category: "Layout",
        icon: FaTag,
        syntax: "<Card title=\"...\" eyebrow=\"...\">...</Card>",
        description: "Single content card for compact grouped information.",
        props: [
            {
                name: "title",
                type: "string",
                description: "Optional card heading.",
            },
            {
                name: "eyebrow",
                type: "string",
                description: "Small label shown above the title.",
            },
            {
                name: "children",
                type: "ReactNode",
                description: "Optional body content rendered inside the card.",
            },
        ],
        example: "<Card title=\"Ort\" eyebrow=\"Location\">S-Gebäude, Hochschule Kempten</Card>",
        preview: (
            <Card title="Ort" eyebrow="Location">
                <p>S-Gebäude, Hochschule Kempten</p>
            </Card>
        ),
    },
    {
        name: "CardGrid",
        slug: "card-grid",
        category: "Layout",
        icon: FaThLarge,
        syntax: "<CardGrid columns={2|3}>...</CardGrid>",
        description: "Creates a responsive grid for cards.",
        props: [
            {
                name: "columns",
                type: "2 | 3",
                defaultValue: "2",
                description: "Number of columns used on larger screens.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Card elements or other content to place inside the grid.",
            },
        ],
        example: "<CardGrid columns={2}>...</CardGrid>",
        preview: (
            <CardGrid>
                <Card title="Community">Turniere, Brettspiele und spontane Aktionen.</Card>
                <Card title="Verpflegung">Snacks, Getränke und Sammelbestellung.</Card>
            </CardGrid>
        ),
    },
    {
        name: "Columns",
        slug: "columns",
        category: "Layout",
        icon: FaColumns,
        syntax: "<Columns columns={2|3}>...</Columns>",
        description: "Places arbitrary content blocks next to each other on desktop.",
        props: [
            {
                name: "columns",
                type: "2 | 3",
                defaultValue: "2",
                description: "Number of desktop columns before collapsing on smaller screens.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Arbitrary content blocks to place next to each other.",
            },
        ],
        example: "<Columns>\n  <div>Links</div>\n  <div>Rechts</div>\n</Columns>",
        preview: (
            <Columns>
                <Card title="Links">Diese Spalte kann Text, Karten oder Tabellen enthalten.</Card>
                <Card title="Rechts">Die Spalten stapeln sich automatisch auf kleineren Screens.</Card>
            </Columns>
        ),
    },
    {
        name: "Stack",
        slug: "stack",
        category: "Layout",
        icon: FaThList,
        syntax: "<Stack>...</Stack>",
        description: "Stacks children vertically with consistent spacing.",
        props: [
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Items that should be laid out vertically with consistent spacing.",
            },
        ],
        example: "<Stack>\n  <Card title=\"A\" />\n  <Card title=\"B\" />\n</Stack>",
        preview: (
            <Stack>
                <Card title="Turnier 1">Just Dance</Card>
                <Card title="Turnier 2">Halfsword 1v1</Card>
            </Stack>
        ),
    },
    {
        name: "Table",
        slug: "table",
        category: "Data and Lists",
        icon: FaTable,
        syntax: "<Table headers={[...]} rows={[[...], [...]]} />",
        description: "General-purpose table for structured tabular data such as schedules or comparisons.",
        props: [
            {
                name: "headers",
                type: "string[]",
                description: "Optional header row labels. Omit when the table should render without a table head.",
            },
            {
                name: "rows",
                type: "string[][]",
                required: true,
                description: "Table body rows. Each inner array represents one row in display order.",
            },
        ],
        example: "<Table headers={[\"Uhrzeit\", \"Programmpunkt\"]} rows={[[\"17:00\", \"Einlass\"], [\"18:30\", \"Start\"]]} />",
        preview: (
            <Table
                headers={["Uhrzeit", "Programmpunkt"]}
                rows={[
                    ["17:00", "Einlass"],
                    ["18:30", "Beginn Ansprache"],
                    ["19:00", "Turnierstart"],
                ]}
            />
        ),
    },
    {
        name: "LinkedImage",
        slug: "linked-image",
        category: "Media and Brand",
        icon: FaImage,
        syntax: "<LinkedImage href=\"...\" src={...} alt=\"...\" caption=\"...\" />",
        description: "Linked image block for sponsors, partners or promo assets.",
        props: [
            {
                name: "href",
                type: "string",
                required: true,
                description: "Target URL opened when the image is clicked.",
            },
            {
                name: "src",
                type: "string",
                required: true,
                description: "Image source, usually imported at the top of the MDX file.",
            },
            {
                name: "alt",
                type: "string",
                required: true,
                description: "Accessible alternative text for the image.",
            },
            {
                name: "caption",
                type: "string",
                description: "Optional caption shown below the linked image.",
            },
        ],
        example: "import sponsorLogo from \"...\";\n<LinkedImage href=\"https://www.soloplan.de\" src={sponsorLogo} alt=\"Soloplan Logo\" caption=\"Unser Hauptsponsor\" />",
        preview: (
            <LinkedImage
                href="https://www.soloplan.de"
                src={sponsorLogo}
                alt="Soloplan Logo"
                caption="Unser Hauptsponsor"
            />
        ),
    },
    {
        name: "Embed",
        slug: "embed",
        category: "Media and Brand",
        icon: FaVideo,
        syntax: "<Embed src=\"...\" title=\"...\" />",
        description: "Responsive iframe embed wrapper for streams, videos or external widgets.",
        props: [
            {
                name: "src",
                type: "string",
                required: true,
                description: "Embed URL used for the iframe source.",
            },
            {
                name: "title",
                type: "string",
                required: true,
                description: "Accessible iframe title.",
            },
        ],
        example: "<Embed src=\"https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ\" title=\"Video\" />",
        preview: (
            <Embed
                src="https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"
                title="Video Beispiel"
            />
        ),
    },
    {
        name: "Text",
        slug: "text",
        category: "Text and Actions",
        icon: FaPenFancy,
        syntax: "<Text as=\"p\" size=\"md\" color=\"secondary\" weight=\"regular\">...</Text>",
        description: "Generic text styling component for palette-based color, font size steps, and weight control.",
        props: [
            {
                name: "as",
                type: "ElementType",
                defaultValue: "\"p\"",
                description: "HTML element to render, for example p, span, strong or div.",
            },
            {
                name: "size",
                type: "\"xs\" | \"sm\" | \"md\" | \"lg\" | \"xl\"",
                defaultValue: "\"md\"",
                description: "Text size step used for visual emphasis.",
            },
            {
                name: "color",
                type: "\"primary\" | \"secondary\" | \"highlight\" | \"special\"",
                defaultValue: "\"primary\"",
                description: "Color token from the site palette.",
            },
            {
                name: "weight",
                type: "\"regular\" | \"medium\" | \"semibold\" | \"bold\"",
                defaultValue: "\"regular\"",
                description: "Font weight emphasis.",
            },
            {
                name: "children",
                type: "ReactNode",
                required: true,
                description: "Text content to render.",
            },
        ],
        example: "<Text size=\"xl\" color=\"highlight\" weight=\"bold\">7€</Text>",
        preview: (
            <Stack>
                <Text size="xl" color="highlight" weight="bold">7 EUR</Text>
                <Text color="secondary">Sekundarer Beschreibungstext mit weniger Gewicht.</Text>
                <Text as="span" size="sm" color="special" weight="semibold">Inline Label</Text>
            </Stack>
        ),
    },
];

export const mdxDocGroups = Object.entries(
    mdxComponentDocs.reduce<Record<string, MdxComponentDoc[]>>((groups, doc) => {
        groups[doc.category] ??= [];
        groups[doc.category].push(doc);
        return groups;
    }, {})
);

export function getMdxComponentDoc(slug?: string) {
    return mdxComponentDocs.find((doc) => doc.slug === slug);
}
