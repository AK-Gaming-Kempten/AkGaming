"""Build the event collage from six complete browser saves of itch.io entries.

Usage: python3 scripts/build-gamejam-collage.py /path/to/browser/saves
Requires Pillow and beautifulsoup4. Source images are never modified.
"""
import argparse
import json
import random
from pathlib import Path
from urllib.parse import unquote

from bs4 import BeautifulSoup
from PIL import Image, ImageDraw, ImageOps


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('saved_pages', type=Path)
    args = parser.parse_args()
    entries = []
    excluded = []
    for jam in range(1, 7):
        page = args.saved_pages / f'Submissions to Ak Gaming Game Jam #{jam} - itch.io.html'
        soup = BeautifulSoup(page.read_text(), 'html.parser')
        cells = soup.select('.game_cell')
        if not cells:
            raise ValueError(f'No submissions found in {page}')
        for cell in cells:
            title = cell.select_one('.label .title')
            entry = {'jam': jam, 'title': title.get_text(strip=True), 'url': title['href']}
            thumbnail = cell.select_one('.game_thumb img')
            if thumbnail is None or cell.select_one('.missing_image'):
                excluded.append(entry)
                continue
            source = page.parent / unquote(thumbnail['src'])
            with Image.open(source) as image:
                rgba = ImageOps.exif_transpose(image).convert('RGBA')
                background = Image.new('RGBA', rgba.size, '#123021')
                entry['image'] = Image.alpha_composite(background, rgba).convert('RGB')
            entries.append(entry)

    # A seeded layout keeps rebuilds stable while mixing jams and breaking up the grid.
    width, height, rows = 1800, 1200, 6
    rng = random.Random(62)
    arranged = entries.copy()
    rng.shuffle(arranged)
    canvas = Image.new('RGB', (width, height))
    coverage = Image.new('L', (width, height))
    coverage_draw = ImageDraw.Draw(coverage)
    # Shared sloping row boundaries meet exactly: no gutters or empty corners.
    boundaries = [(0, 0)]
    for row in range(1, rows):
        center = row * height // rows + rng.randint(-24, 24)
        slope = rng.choice([-1, 1]) * rng.randint(20, 40)
        boundaries.append((center - slope, center + slope))
    boundaries.append((height, height))
    index = 0
    for row in range(rows):
        count = len(entries) // rows + (row < len(entries) % rows)
        weights = [rng.uniform(.8, 1.3) for _ in range(count)]
        edges = [0]
        for weight in weights:
            edges.append(edges[-1] + weight / sum(weights) * width)
        edges[-1] = width
        upper, lower = boundaries[row], boundaries[row + 1]
        def boundary_y(boundary, x):
            return round(boundary[0] + (boundary[1] - boundary[0]) * x / width)
        for column in range(count):
            left, right = round(edges[column]), round(edges[column + 1])
            points = [(left, boundary_y(upper, left)), (right, boundary_y(upper, right)),
                      (right, boundary_y(lower, right)), (left, boundary_y(lower, left))]
            top, bottom = min(y for _, y in points) - 3, max(y for _, y in points) + 3
            left, right = left - 3, right + 3
            size = (right - left, bottom - top)
            # Tilt the artwork itself as well as the cuts; fill-crop prevents letterboxing.
            angle = rng.uniform(-9, 9)
            tile = ImageOps.fit(arranged[index]['image'], (size[0] + 90, size[1] + 90), Image.Resampling.LANCZOS)
            tile = tile.rotate(angle, resample=Image.Resampling.BICUBIC)
            tile = ImageOps.fit(tile, size, Image.Resampling.LANCZOS)
            mask = Image.new('L', size)
            local_points = [(x - left, y - top) for x, y in points]
            mask_draw = ImageDraw.Draw(mask)
            mask_draw.polygon(local_points, fill=255)
            mask_draw.line(local_points + [local_points[0]], fill=255, width=5)
            canvas.paste(tile, (left, top), mask)
            coverage_draw.polygon(points, fill=255)
            coverage_draw.line(points + [points[0]], fill=255, width=5)
            index += 1
    assert index == len(entries)
    assert coverage.getextrema() == (255, 255), 'Collage has an uncovered gap'
    root = Path(__file__).resolve().parents[1]
    output = root / 'public/media/eventInfoGallery/gamejam'
    output.mkdir(parents=True, exist_ok=True)
    canvas.save(output / 'gamejam-1-6-collage.webp', quality=90, method=6)
    manifest = {'included': [{k: v for k, v in entry.items() if k != 'image'} for entry in entries], 'excludedWithoutThumbnail': excluded}
    (root / 'src/data/gamejam-collage-sources.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    print(f'Created collage with {len(entries)} custom thumbnails; excluded {len(excluded)} placeholders.')


if __name__ == '__main__':
    main()
