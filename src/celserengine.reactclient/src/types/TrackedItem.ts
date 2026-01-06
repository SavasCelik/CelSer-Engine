export type TrackedItem = {
  description: string;
  address: string;
  value: string;
  dataType: number;
  isPointer: boolean;
  moduleNameWithBaseOffset: string;
  offsets: string[];
  pointingTo: string;
};
