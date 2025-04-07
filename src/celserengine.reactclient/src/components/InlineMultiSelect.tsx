import {
  Group,
  Input,
  MantineSize,
  MultiSelect,
  MultiSelectProps,
} from "@mantine/core";
import { useId } from "@mantine/hooks";

const spacing: Record<MantineSize, string> = {
  xs: "6px",
  sm: "8px",
  md: "12px",
  lg: "16px",
  xl: "20px",
};

export default function InlineSelect(props: MultiSelectProps) {
  const uuid = useId(props.id);
  return (
    <>
      <Group align="normal" gap="xs">
        {props.label && (
          <Input.Label
            key="label"
            size={props.size}
            w={props.labelProps?.width}
            required={props.required}
            htmlFor={uuid}
            style={{ marginTop: spacing[(props.size as MantineSize) || "sm"] }}
          >
            {props.label}
          </Input.Label>
        )}
        <MultiSelect
          flex={1}
          {...{
            ...props,
            label: undefined,
            id: uuid,
          }}
        />
      </Group>
    </>
  );
}
