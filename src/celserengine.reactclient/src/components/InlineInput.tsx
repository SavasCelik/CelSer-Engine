import {
  Group,
  Input,
  MantineSize,
  TextInput,
  TextInputProps,
} from "@mantine/core";
import { useId } from "@mantine/hooks";

const spacing: Record<MantineSize, string> = {
  xs: "6px",
  sm: "8px",
  md: "12px",
  lg: "16px",
  xl: "20px",
};

export default function InlineInput(props: TextInputProps) {
  const uuid = useId();

  return (
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
      <TextInput {...{ ...props, label: undefined, id: uuid }} />
    </Group>
  );
}
