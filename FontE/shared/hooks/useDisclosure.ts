import { useCallback, useState } from "react";

export interface UseDisclosureReturn {
  isOpen: boolean;
  onOpen: () => void;
  onClose: () => void;
  onToggle: () => void;
}

/**
 * Standard hook to manage open/close state of modals, drawers, and popovers.
 */
export function useDisclosure(initialState: boolean = false): UseDisclosureReturn {
  const [isOpen, setIsOpen] = useState(initialState);

  const onOpen = useCallback(() => setIsOpen(true), []);
  const onClose = useCallback(() => setIsOpen(false), []);
  const onToggle = useCallback(() => setIsOpen((prev) => !prev), []);

  return { isOpen, onOpen, onClose, onToggle };
}
