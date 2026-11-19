export function getPatientInitials(fullNameValue?: string): string {
  if (!fullNameValue) {
    return "?";
  }

  const parts = fullNameValue.trim().split(" ").filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  if (parts.length === 1) {
    return parts[0].charAt(0).toUpperCase();
  }

  return `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase();
}
