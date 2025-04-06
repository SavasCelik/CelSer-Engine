import { Group, Input, MantineSize, Select, SelectProps } from "@mantine/core";
import { useId } from "@mantine/hooks";

const spacing: Record<MantineSize, string> = {
  xs: "6px",
  sm: "8px",
  md: "12px",
  lg: "16px",
  xl: "20px",
};

export default function InlineSelect(props: SelectProps) {
  const uuid = useId(props.id);
  return (
    <>
      <Group align="normal" gap="xs">
        {props.label && (
          <Input.Label
            key="label"
            size={props.size}
            required={props.required}
            htmlFor={uuid}
            style={{ marginTop: spacing[(props.size as MantineSize) || "sm"] }}
          >
            {props.label}
          </Input.Label>
        )}
        <Select
          {...{
            ...props,
            label: undefined,
            id: uuid,
          }} /* styles={{ root: { flex: 1 } }} // Ensures the select takes remaining space */
        />
      </Group>
    </>
  );
}
