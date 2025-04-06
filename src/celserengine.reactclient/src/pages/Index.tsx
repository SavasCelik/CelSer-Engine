import {
  TextInput,
  Switch,
  Button,
  useMantineTheme,
  CloseButton,
  Kbd,
  Input,
  Select,
  Text,
  Group,
  Space,
  Box,
  Stack,
  Table,
} from "@mantine/core";
import { useId } from "@mantine/hooks";
import { IconAt, IconSearch, IconX } from "@tabler/icons-react";
import { useState } from "react";
import InlineInput from "../components/InlineInput";
import InlineSelect from "../components/InlineSelect";
import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels";
import ScanResultTable from "./ScanResultTable";

function Index() {
  const [searchValue, setSearchValue] = useState("lol");

  return (
    <>
      <Group wrap="nowrap">
        <ScanResultTable />
        <TextInput
          size="xs"
          placeholder="Search value"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          leftSectionPointerEvents="none"
          leftSection={<IconSearch size={16} />}
          rightSectionPointerEvents="all"
          w={200}
          rightSection={
            searchValue && (
              <CloseButton
                tabIndex={-1}
                size="sm"
                onClick={() => setSearchValue("")}
              />
            )
          }
        />
      </Group>
    </>
  );
}

export default Index;
