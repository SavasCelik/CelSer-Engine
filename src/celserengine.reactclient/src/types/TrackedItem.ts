export type TrackedItem = {
  description: string;
  address: string;
  value: string;
  isPointer: boolean;
  moduleNameWithBaseOffset: string;
  offsets: string[];
  pointingTo: string;
};
