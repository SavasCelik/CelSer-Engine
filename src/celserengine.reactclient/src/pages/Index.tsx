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
  Flex,
  ActionIcon,
  Progress,
  RingProgress,
  Center,
  SimpleGrid,
  Fieldset,
  MultiSelect,
  Divider,
} from "@mantine/core";
import { useId } from "@mantine/hooks";
import { IconAt, IconDevicesPc, IconSearch, IconX } from "@tabler/icons-react";
import { useState, useRef, useEffect } from "react";
import InlineInput from "../components/InlineInput";
import InlineSelect from "../components/InlineSelect";
import {
  ImperativePanelHandle,
  Panel,
  PanelGroup,
  PanelResizeHandle,
} from "react-resizable-panels";
import ScanResultTable from "./ScanResultTable";
import Test from "./Test";
import InlineMultiSelect from "../components/InlineMultiSelect";
import NewScanResultTable from "./NewScanResultTable";

function Index() {
  const [searchValue, setSearchValue] = useState("lol");
  const scanType = ["Exact Value", "Pattern Scan", "Signature Scan"];
  const valueType = [
    "Integer (4 Bytes)",
    "Float (4 Bytes)",
    "Double (8 Bytes)",
  ];
  const processModules = ["All", "OneDrive.exe"];
  const memoryManipulation = ["Yes", "No", "Don't Care"];
  const memoryTypes = ["Image", "Private", "Mapped"];

  const [minPanelHeight, setMinPanelHeight] = useState(0);
  const stackRef = useRef<HTMLDivElement>(null);
  const panelGroupRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const updateMinPanelHeight = () => {
      if (stackRef.current && panelGroupRef.current) {
        setMinPanelHeight(
          (stackRef.current.offsetHeight * 100) /
            panelGroupRef.current.offsetHeight
        );
      }
    };

    updateMinPanelHeight();
    window.addEventListener("resize", updateMinPanelHeight);

    return () => {
      window.removeEventListener("resize", updateMinPanelHeight);
    };
  }, []);

  return (
    <>
      <Stack p={6} gap={6} h="100vh">
        <Group>
          <ActionIcon size="lg">
            <IconDevicesPc stroke={2} />
          </ActionIcon>
          <Stack flex={1} gap={0}>
            <Center>
              <Text size="xs">0x00006AC4 - OneDrive.exe</Text>
            </Center>
            <Progress value={50} h={15} />
          </Stack>
        </Group>
        <Box ref={panelGroupRef} h="100%">
          <PanelGroup direction="vertical">
            <Panel minSize={minPanelHeight} data-my-size={minPanelHeight}>
              <Group align="flex-start" gap={6}>
                <ScanResultTable />
                <Stack ref={stackRef} w={325} gap={6} pt="md" pb={10}>
                  <TextInput
                    size="xs"
                    placeholder="Search value"
                    value={searchValue}
                    onChange={(e) => setSearchValue(e.target.value)}
                    leftSectionPointerEvents="none"
                    leftSection={<IconSearch size={16} />}
                    rightSectionPointerEvents="all"
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
                  <SimpleGrid cols={3} spacing={5} mb={10}>
                    <Button size="xs">First Scan</Button>
                    <Button size="xs" variant="outline">
                      New Scan
                    </Button>
                  </SimpleGrid>
                  <InlineSelect
                    size="xs"
                    label="Scan Type"
                    labelProps={{ width: 60 }}
                    data={scanType}
                    defaultValue={scanType[0]}
                  />
                  <InlineSelect
                    size="xs"
                    label="Value Type"
                    labelProps={{ width: 60 }}
                    data={valueType}
                    defaultValue={valueType[0]}
                  />
                  <Fieldset variant="unstyled" m={0}>
                    <Divider
                      mt="xs"
                      mb={6}
                      size="sm"
                      label={
                        <Text size="xs" c="var(--mantine-color-text)">
                          Memory Scan Options
                        </Text>
                      }
                      labelPosition="center"
                    />
                    <Stack gap={6}>
                      <Select
                        size="xs"
                        searchable
                        data={processModules}
                        defaultValue={processModules[0]}
                      />
                      <InlineInput size="xs" label="Start" defaultValue="0" />
                      <InlineInput
                        size="xs"
                        label="Stop"
                        defaultValue="7FFFFFFFFFFFFFFF"
                      />
                      <SimpleGrid cols={3} spacing={6}>
                        <Select
                          size="xs"
                          label="Writeable"
                          data={memoryManipulation}
                          defaultValue={memoryManipulation[0]}
                        />
                        <Select
                          size="xs"
                          label="Executable"
                          data={memoryManipulation}
                          defaultValue={memoryManipulation[2]}
                        />
                        <Select
                          size="xs"
                          label="Copy On Write"
                          data={memoryManipulation}
                          defaultValue={memoryManipulation[1]}
                        />
                      </SimpleGrid>
                      <InlineMultiSelect
                        size="xs"
                        label="Memory Types"
                        data={memoryTypes}
                        defaultValue={[memoryTypes[0], memoryTypes[1]]}
                      />
                    </Stack>
                  </Fieldset>
                </Stack>
              </Group>
            </Panel>
            <PanelResizeHandle />
            <Panel>
              <NewScanResultTable />
            </Panel>
          </PanelGroup>
        </Box>
      </Stack>
    </>
  );
}

export default Index;
