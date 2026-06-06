const CP1252_REVERSE = new Map<string, number>(
  [
    [0x20ac, 0x80],
    [0x201a, 0x82],
    [0x0192, 0x83],
    [0x201e, 0x84],
    [0x2026, 0x85],
    [0x2020, 0x86],
    [0x2021, 0x87],
    [0x02c6, 0x88],
    [0x2030, 0x89],
    [0x0160, 0x8a],
    [0x2039, 0x8b],
    [0x0152, 0x8c],
    [0x017d, 0x8e],
    [0x2018, 0x91],
    [0x2019, 0x92],
    [0x201c, 0x93],
    [0x201d, 0x94],
    [0x2022, 0x95],
    [0x2013, 0x96],
    [0x2014, 0x97],
    [0x02dc, 0x98],
    [0x2122, 0x99],
    [0x0161, 0x9a],
    [0x203a, 0x9b],
    [0x0153, 0x9c],
    [0x017e, 0x9e],
    [0x0178, 0x9f],
  ].map(([unicodeCodePoint, byte]) => [String.fromCodePoint(unicodeCodePoint), byte])
);

function looksLikeMojibake(value: string): boolean {
  return Array.from(value).some((char) => {
    const code = char.codePointAt(0) ?? 0;
    return (
      (code >= 0x80 && code <= 0x9f) ||
      code === 0x00c2 ||
      code === 0x00c3 ||
      code === 0x00c4 ||
      code === 0x00c5 ||
      code === 0x00c6 ||
      code === 0x00f0
    );
  });
}

export function normalizeVietnameseText(value: string | null | undefined): string | null {
  if (!value) {
    return value ?? null;
  }

  if (!looksLikeMojibake(value)) {
    return value;
  }

  const bytes: number[] = [];
  for (const char of value) {
    const code = char.codePointAt(0) ?? 0;
    if (code <= 0xff) {
      bytes.push(code);
    } else {
      const cp1252Byte = CP1252_REVERSE.get(char);
      if (cp1252Byte === undefined) {
        return value;
      }
      bytes.push(cp1252Byte);
    }
  }

  const decoded = new TextDecoder("utf-8", { fatal: false }).decode(
    Uint8Array.from(bytes)
  );

  return decoded.includes("\uFFFD") ? value : decoded;
}
