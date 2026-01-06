import { useAuth0 } from "@auth0/auth0-react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import {
  Navbar,
  NavbarBrand,
  Dropdown,
  DropdownHeader,
  DropdownItem,
  DropdownDivider,
  Avatar,
  Button,
  Spinner,
  DarkThemeToggle,
} from "flowbite-react";

export default function Header() {
  const { isLoading, isAuthenticated, user, loginWithRedirect, logout } =
    useAuth0();

  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout({
      logoutParams: {
        returnTo: window.location.origin,
      },
    });
  };

  return (
    <Navbar fluid className="border-b border-slate-200 dark:border-gray-700">
      <NavbarBrand as={Link} to="/">
        <img
          src="/cloud.svg"
          className="mr-3 h-6 sm:h-9"
          alt="MycroCloud Logo"
        />
        <span className="self-center text-xl font-semibold whitespace-nowrap dark:text-white">
          MycroCloud
        </span>
      </NavbarBrand>

      {/* Left-side navigation */}
      {isAuthenticated && (
        <div className="flex ml-6 gap-4">
          <Link
            to="/apps"
            className={`py-2 ${
              location.pathname.startsWith("/apps")
                ? "text-blue-700 dark:text-blue-500 font-medium"
                : "text-gray-700 dark:text-gray-400 hover:text-blue-700 dark:hover:text-blue-500"
            }`}
          >
            Apps
          </Link>
        </div>
      )}

      <div className="flex ml-auto items-center gap-4">
        <a
          href="https://docs.mycrocloud.info"
          target="_blank"
          rel="noopener noreferrer"
          className="text-blue-600 dark:text-blue-400 hover:underline"
        >
          Docs
        </a>
        <DarkThemeToggle />
        {isLoading && <Spinner aria-label="Loading..." size="sm" />}

        {!isLoading && !isAuthenticated && (
          <Button color="blue" onClick={() => loginWithRedirect()}>
            Login
          </Button>
        )}

        {!isLoading && isAuthenticated && user && (
          <Dropdown
            arrowIcon={false}
            inline
            label={<Avatar img={user.picture} alt="User avatar" rounded />}
          >
            <DropdownHeader>
              <span className="block text-sm">{user.name}</span>
              <span className="block truncate text-sm font-medium">
                {user.email}
              </span>
            </DropdownHeader>
            <DropdownItem onClick={() => navigate("/settings")}>
              Settings
            </DropdownItem>
            <DropdownDivider />
            <DropdownItem onClick={handleLogout}>Sign out</DropdownItem>
          </Dropdown>
        )}
      </div>
    </Navbar>
  );
}
