import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
    CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { Check, CreditCard, Download, Zap } from "lucide-react";

export default function Billing() {
    return (
        <div className="space-y-8">
            {/* Current Plan & Usage */}
            <section className="space-y-4">
                <h2 className="text-xl font-semibold">Current Plan</h2>
                <Card>
                    <CardHeader>
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle className="text-lg">Hobby Plan</CardTitle>
                                <CardDescription>Perfect for side projects and learning</CardDescription>
                            </div>
                            <Badge variant="secondary" className="text-sm">
                                Current
                            </Badge>
                        </div>
                    </CardHeader>
                    <CardContent className="space-y-6">
                        <div className="space-y-2">
                            <div className="flex items-center justify-between text-sm">
                                <span className="font-medium">Total Requests</span>
                                <span className="text-muted-foreground">8,432 / 100,000</span>
                            </div>
                            <Progress value={8.4} className="h-2" />
                        </div>
                        <div className="space-y-2">
                            <div className="flex items-center justify-between text-sm">
                                <span className="font-medium">Function Duration</span>
                                <span className="text-muted-foreground">12m 4s / 10h</span>
                            </div>
                            <Progress value={2} className="h-2" />
                        </div>
                    </CardContent>
                    <CardFooter className="border-t bg-muted/50 px-6 py-4">
                        <div className="flex w-full items-center justify-between">
                            <span className="text-sm text-muted-foreground">
                                Plan renews on March 1, 2026
                            </span>
                            <Button variant="outline" size="sm">
                                Manage Subscription
                            </Button>
                        </div>
                    </CardFooter>
                </Card>
            </section>

            {/* Available Plans */}
            <section className="space-y-4">
                <h2 className="text-xl font-semibold">Available Plans</h2>
                <div className="grid gap-6 md:grid-cols-2">
                    {/* Hobby */}
                    <Card className="flex flex-col border-muted bg-muted/20">
                        <CardHeader>
                            <CardTitle>Hobby</CardTitle>
                            <CardDescription>
                                <span className="text-2xl font-bold text-foreground">$0</span> / month
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="flex-1 space-y-4">
                            <ul className="space-y-2 text-sm">
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    100k requests / month
                                </li>
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    10 hours function duration
                                </li>
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    Community support
                                </li>
                            </ul>
                        </CardContent>
                        <CardFooter>
                            <Button disabled className="w-full">
                                Current Plan
                            </Button>
                        </CardFooter>
                    </Card>

                    {/* Pro */}
                    <Card className="flex flex-col border-primary shadow-sm hover:shadow-md transition-shadow">
                        <CardHeader>
                            <div className="flex items-center justify-between">
                                <CardTitle>Pro</CardTitle>
                                <Zap className="h-5 w-5 text-yellow-500 fill-yellow-500" />
                            </div>
                            <CardDescription>
                                <span className="text-2xl font-bold text-foreground">$29</span> / month
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="flex-1 space-y-4">
                            <ul className="space-y-2 text-sm">
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    Unlimited requests
                                </li>
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    1,000 hours function duration
                                </li>
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    Priority support
                                </li>
                                <li className="flex items-center gap-2">
                                    <Check className="h-4 w-4 text-primary" />
                                    Private repositories
                                </li>
                            </ul>
                        </CardContent>
                        <CardFooter>
                            <Button className="w-full">Upgrade to Pro</Button>
                        </CardFooter>
                    </Card>
                </div>
            </section>

            {/* Payment Methods */}
            <section className="space-y-4">
                <h2 className="text-xl font-semibold">Payment Methods</h2>
                <Card>
                    <CardContent className="flex items-center justify-between p-6">
                        <div className="flex items-center gap-4">
                            <div className="rounded border p-2">
                                <CreditCard className="h-6 w-6 text-muted-foreground" />
                            </div>
                            <div>
                                <p className="font-medium">Visa ending in 4242</p>
                                <p className="text-sm text-muted-foreground">Expires 12/28</p>
                            </div>
                        </div>
                        <Button variant="ghost" size="sm">
                            Edit
                        </Button>
                    </CardContent>
                </Card>
            </section>

            {/* Billing History */}
            <section className="space-y-4">
                <h2 className="text-xl font-semibold">Billing History</h2>
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Date</TableHead>
                                <TableHead>Amount</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead className="text-right">Invoice</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {[
                                { date: "Feb 1, 2026", amount: "$0.00", status: "Paid" },
                                { date: "Jan 1, 2026", amount: "$0.00", status: "Paid" },
                                { date: "Dec 1, 2025", amount: "$0.00", status: "Paid" },
                            ].map((invoice, i) => (
                                <TableRow key={i}>
                                    <TableCell>{invoice.date}</TableCell>
                                    <TableCell>{invoice.amount}</TableCell>
                                    <TableCell>
                                        <Badge variant="outline" className="font-normal text-muted-foreground">
                                            {invoice.status}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Button variant="ghost" size="icon">
                                            <Download className="h-4 w-4 opacity-50" />
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
            </section>
        </div>
    );
}
