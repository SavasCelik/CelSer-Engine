import { useParams } from "react-router";

export default function NotFoundPage() {
  const page = useParams<{ "*": string }>();

  return <h1>Page not Found! Requestet Page: {page["*"]}</h1>;
}
