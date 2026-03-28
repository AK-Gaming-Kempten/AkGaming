type TableRow = Array<string>;

type TableProps = {
    headers?: string[];
    rows: TableRow[];
};

export default function Table({ headers, rows }: TableProps) {
    return (
        <div className="mdx-table-wrap">
            <table className="mdx-table">
                {headers !== undefined ? (
                    <thead>
                        <tr>
                            {headers.map((header) => (
                                <th key={header}>{header}</th>
                            ))}
                        </tr>
                    </thead>
                ) : null}
                <tbody>
                    {rows.map((row, rowIndex) => (
                        <tr key={`row-${rowIndex}`}>
                            {row.map((cell, cellIndex) => (
                                <td key={`cell-${rowIndex}-${cellIndex}`}>{cell}</td>
                            ))}
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
