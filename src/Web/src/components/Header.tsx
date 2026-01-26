import { useAuth0 } from "@auth0/auth0-react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
const isDevMode = import.meta.env.DEV;

function Header() {
  const {
    isLoading,
    isAuthenticated,
    user,
    loginWithRedirect,
    logout,
    getAccessTokenSilently,
  } = useAuth0();
  const navigate = useNavigate();

  const handleCopyAccessTokenClick = async () => {
    const accessToken = await getAccessTokenSilently();
    navigator.clipboard.writeText(accessToken);
  };

  if (isLoading) {
    return null;
  }

  return (
    <nav className="flex items-center border-b-[1px] border-b-gray-300 px-8 py-2">
      <Link to="/" className="">
        <span className="self-center whitespace-nowrap text-xl font-semibold">
          MycroCloud
        </span>
      </Link>
      {isAuthenticated ? (
        <>
          <ul className="me-auto ms-4 flex space-x-2">
            <li>
              <Link
                className="block border-b border-gray-100 py-2 pl-3 pr-4 text-gray-700 hover:bg-gray-50 md:border-0 md:p-0 md:hover:bg-transparent md:hover:text-cyan-700"
                to="apps"
              >
                Apps
              </Link>
            </li>
          </ul>
          <a
            className="me-2 ms-auto block border-b border-gray-100 py-2 pl-3 pr-4 text-gray-700 hover:bg-gray-50 md:border-0 md:p-0 md:hover:bg-transparent md:hover:text-cyan-700"
            href="https://docs.mycrocloud.info" // todo: use env variable
            rel="noopener noreferrer"
            target="_blank"
          >
            Docs
          </a>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="relative h-8 w-8 rounded-full">
                <Avatar className="h-8 w-8">
                  <AvatarImage src={user?.picture} alt="User settings" />
                  <AvatarFallback>{user?.name?.charAt(0) ?? "U"}</AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>
                <span className="block">{user?.name}</span>
                <span className="block truncate text-sm font-normal text-muted-foreground">{user?.email}</span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => navigate("/settings")}>
                Account Settings
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => logout()}>Log out</DropdownMenuItem>
              {isDevMode && (
                <DropdownMenuItem onClick={handleCopyAccessTokenClick}>
                  Copy access token
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </>
      ) : (
        <Button className="ms-auto" size="sm" onClick={() => loginWithRedirect()}>
          Log in
        </Button>
      )}
    </nav>
  );
}
export default Header;
