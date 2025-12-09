import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { APITester } from "./APITester";

export function Debug() {
  return (
    <div className="container mx-auto p-8 text-center relative z-10">
      <Card>
        <CardHeader className="gap-4">
          <CardTitle className="text-3xl font-bold">Bun + React</CardTitle>
          <CardDescription>
            Edit{" "}
            <code className="rounded bg-muted px-[0.3rem] py-[0.2rem] font-mono">
              src/App.tsx
            </code>{" "}
            and save to test HMR
          </CardDescription>
        </CardHeader>
        <CardContent>
          <APITester />
        </CardContent>
      </Card>
    </div>
  );
}
