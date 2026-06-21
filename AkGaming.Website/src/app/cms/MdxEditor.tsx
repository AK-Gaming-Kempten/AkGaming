"use client";

import { useRef } from "react";
import CodeMirror from "@uiw/react-codemirror";
import { markdown } from "@codemirror/lang-markdown";
import { RangeSetBuilder } from "@codemirror/state";
import { Decoration, type DecorationSet, EditorView, hoverTooltip, type ViewUpdate, ViewPlugin } from "@codemirror/view";
import {
    LuBold,
    LuCode,
    LuHeading1,
    LuHeading2,
    LuHeading3,
    LuItalic,
    LuLink,
    LuList,
    LuListOrdered,
    LuQuote,
    LuTable,
} from "react-icons/lu";
import { mdxComponentDocs } from "../../components/content/mdxCatalog";

type MdxEditorProps = {
    value: string;
    onChange: (value: string) => void;
};

type ComponentTemplate = {
    label: string;
    value: string;
    template: string;
};

const componentTemplates: ComponentTemplate[] = [
    { label: "Lead", value: "lead", template: "<Lead>Lead text</Lead>" },
    { label: "Callout", value: "callout", template: "<Callout title=\"Title\" tone=\"accent\">\n  Important content\n</Callout>" },
    { label: "Card", value: "card", template: "<Card title=\"Title\" eyebrow=\"Label\">\n  Card content\n</Card>" },
    { label: "Card grid", value: "card-grid", template: "<CardGrid columns={2}>\n  <Card title=\"First card\">Content</Card>\n  <Card title=\"Second card\">Content</Card>\n</CardGrid>" },
    { label: "Columns", value: "columns", template: "<Columns columns={2}>\n  Left column\n  \n  Right column\n</Columns>" },
    { label: "Button", value: "button-link", template: "<ButtonLink href=\"/events\">Button label</ButtonLink>" },
    { label: "Button row", value: "button-row", template: "<ButtonRow>\n  <ButtonLink href=\"/events\">Events</ButtonLink>\n</ButtonRow>" },
    { label: "Image", value: "linked-image", template: "<LinkedImage href=\"https://example.com\" src=\"/media/image.png\" alt=\"Description\" />" },
    { label: "Text", value: "text", template: "<Text color=\"secondary\">Supporting text</Text>" },
    { label: "Stack", value: "stack", template: "<Stack>\n  Content\n</Stack>" },
    { label: "Table", value: "table", template: "<Table headers={[\"Column 1\", \"Column 2\"]} rows={[[\"Value 1\", \"Value 2\"]]} />" },
    { label: "Embed", value: "embed", template: "<Embed src=\"https://www.youtube-nocookie.com/embed/VIDEO_ID\" title=\"Video title\" />" },
];

const componentNames = new Set([
    "Lead", "ButtonLink", "ButtonRow", "Callout", "Card", "CardGrid", "Columns", "Stack", "Table", "LinkedImage", "Embed", "Text",
]);
const componentDocsByName = new Map(mdxComponentDocs.map(component => [component.name, component]));

type ComponentTagMatch = {
    name: string;
    from: number;
    to: number;
};

function findComponentTag(view: EditorView, position: number): ComponentTagMatch | null {
    const componentTagPattern = /<\/?([A-Z][A-Za-z0-9]*)\b[^>]*>/g;
    const text = view.state.doc.toString();
    let match: RegExpExecArray | null;

    while ((match = componentTagPattern.exec(text)) !== null) {
        const from = match.index;
        const to = from + match[0].length;
        if (position >= from && position <= to && componentNames.has(match[1])) {
            return { name: match[1], from, to };
        }
    }

    return null;
}

function componentDecorations(view: EditorView): DecorationSet {
    const builder = new RangeSetBuilder<Decoration>();
    const componentTagPattern = /<\/?([A-Z][A-Za-z0-9]*)\b[^>]*>/g;
    const text = view.state.doc.toString();
    let match: RegExpExecArray | null;

    while ((match = componentTagPattern.exec(text)) !== null) {
        const componentName = match[1];
        if (!componentNames.has(componentName)) continue;

        builder.add(
            match.index,
            match.index + match[0].length,
            Decoration.mark({
                class: "cm-mdx-component-token",
            }),
        );
    }

    return builder.finish();
}

const mdxComponentHighlighter = ViewPlugin.fromClass(class {
    decorations: DecorationSet;

    constructor(view: EditorView) {
        this.decorations = componentDecorations(view);
    }

    update(update: ViewUpdate) {
        if (update.docChanged) this.decorations = componentDecorations(update.view);
    }
}, {
    decorations: value => value.decorations,
});

const mdxComponentTooltip = hoverTooltip((view, position) => {
    const tag = findComponentTag(view, position);
    if (tag === null) return null;

    const component = componentDocsByName.get(tag.name);
    if (component === undefined) return null;

    return {
        pos: tag.from,
        end: tag.to,
        above: true,
        create() {
            const link = document.createElement("a");
            link.className = "cms-mdx-component-tooltip";
            link.href = `/mdx-docs/${component.slug}`;
            link.target = "_blank";
            link.rel = "noreferrer";

            const title = document.createElement("strong");
            title.textContent = component.name;
            const syntax = document.createElement("code");
            syntax.textContent = component.syntax;
            const description = document.createElement("span");
            description.textContent = component.description;
            const action = document.createElement("span");
            action.className = "cms-mdx-component-tooltip-action";
            action.textContent = "Open documentation ↗";

            link.append(title, syntax, description, action);
            return { dom: link };
        },
    };
}, { hoverTime: 200, hideOnChange: true });

export default function MdxEditor({ value, onChange }: MdxEditorProps) {
    const editorView = useRef<EditorView | null>(null);

    function insertText(createText: (selectedText: string) => string) {
        const view = editorView.current;
        if (!view) return;

        const selection = view.state.selection.main;
        const selectedText = view.state.sliceDoc(selection.from, selection.to);
        const text = createText(selectedText);
        const selectionStart = selection.from;
        const selectionEnd = selection.from + text.length;

        view.dispatch({
            changes: { from: selection.from, to: selection.to, insert: text },
            selection: { anchor: selectionStart, head: selectionEnd },
        });
        view.focus();
    }

    function wrapSelection(before: string, after: string, placeholder: string) {
        const view = editorView.current;
        if (!view) return;

        const selection = view.state.selection.main;
        const selectedText = view.state.sliceDoc(selection.from, selection.to) || placeholder;
        const text = `${before}${selectedText}${after}`;

        view.dispatch({
            changes: { from: selection.from, to: selection.to, insert: text },
            selection: {
                anchor: selection.from + before.length,
                head: selection.from + before.length + selectedText.length,
            },
        });
        view.focus();
    }

    function insertHeading(level: number) {
        insertText(selectedText => `${"#".repeat(level)} ${selectedText || "Heading"}\n\n`);
    }

    function insertList(ordered: boolean) {
        insertText(selectedText => {
            if (!selectedText) return ordered ? "1. List item\n" : "- List item\n";
            return selectedText.split("\n").map((line, index) => ordered ? `${index + 1}. ${line}` : `- ${line}`).join("\n");
        });
    }

    function insertComponent(value: string) {
        const component = componentTemplates.find(template => template.value === value);
        if (!component) return;

        insertText(() => `\n${component.template}\n`);
    }

    return (
        <div className="cms-mdx-editor-pane">
            <div className="cms-mdx-toolbar" aria-label="MDX formatting controls">
                <button type="button" onClick={() => insertHeading(1)} title="Heading 1" aria-label="Heading 1"><LuHeading1 /></button>
                <button type="button" onClick={() => insertHeading(2)} title="Heading 2" aria-label="Heading 2"><LuHeading2 /></button>
                <button type="button" onClick={() => insertHeading(3)} title="Heading 3" aria-label="Heading 3"><LuHeading3 /></button>
                <span className="cms-mdx-toolbar-separator" />
                <button type="button" onClick={() => wrapSelection("**", "**", "bold text")} title="Bold" aria-label="Bold"><LuBold /></button>
                <button type="button" onClick={() => wrapSelection("*", "*", "italic text")} title="Italic" aria-label="Italic"><LuItalic /></button>
                <button type="button" onClick={() => wrapSelection("[", "](https://)", "Link text")} title="Link" aria-label="Link"><LuLink /></button>
                <button type="button" onClick={() => wrapSelection("`", "`", "code")} title="Inline code" aria-label="Inline code"><LuCode /></button>
                <span className="cms-mdx-toolbar-separator" />
                <button type="button" onClick={() => insertList(false)} title="Bulleted list" aria-label="Bulleted list"><LuList /></button>
                <button type="button" onClick={() => insertList(true)} title="Numbered list" aria-label="Numbered list"><LuListOrdered /></button>
                <button type="button" onClick={() => insertText(selectedText => `> ${selectedText || "Quote"}\n`)} title="Quote" aria-label="Quote"><LuQuote /></button>
                <button type="button" onClick={() => insertText(() => "| Column 1 | Column 2 |\n| --- | --- |\n| Value | Value |\n")} title="Table" aria-label="Table"><LuTable /></button>
                <select aria-label="Insert AK Gaming MDX component" defaultValue="" onChange={event => { insertComponent(event.target.value); event.currentTarget.value = ""; }}>
                    <option value="" disabled>Insert component…</option>
                    {componentTemplates.map(component => <option key={component.value} value={component.value}>{component.label}</option>)}
                </select>
            </div>
            <CodeMirror
                className="cms-mdx-code-editor"
                value={value}
                height="100%"
                theme="dark"
                extensions={[markdown(), EditorView.lineWrapping, mdxComponentHighlighter, mdxComponentTooltip]}
                onCreateEditor={view => { editorView.current = view; }}
                onChange={onChange}
                placeholder="Write MDX content…"
            />
        </div>
    );
}
