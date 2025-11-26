import { useAuth0 } from "@auth0/auth0-react";
import { Link, useNavigate } from "react-router-dom";
import {
  Navbar,
  NavbarBrand,
  NavbarCollapse,
  NavbarToggle,
  NavbarLink,
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
  const {
    isLoading,
    isAuthenticated,
    user,
    loginWithRedirect,
    logout,
  } = useAuth0();

  const navigate = useNavigate();

  return (
    <Navbar fluid rounded>
      <NavbarBrand as={Link} href="/">
        <img src="/cloud.svg" className="mr-3 h-6 sm:h-9" alt="MycroCloud Logo" />
        <span className="self-center whitespace-nowrap text-xl font-semibold dark:text-white">
          MycroCloud
        </span>
      </NavbarBrand>

      <div className="flex gap-4 md:order-2 items-center list-none">
        <NavbarLink
          href="https://mycrocloud.docs.example.com"
          target="_blank"
          rel="noopener noreferrer"
          className="text-blue-600 dark:text-blue-400"
        >
          Docs
        </NavbarLink>
        <DarkThemeToggle />
        {isLoading && (
          <Spinner aria-label="Loading.." />
        )}

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
              <span className="block truncate text-sm font-medium">{user.email}</span>
            </DropdownHeader>
            <DropdownItem onClick={() => navigate("/settings")}>Settings</DropdownItem>
            <DropdownDivider />
            <DropdownItem
              onClick={() => logout()}
            >
              Sign out
            </DropdownItem>
          </Dropdown>
        )}
        <NavbarToggle />
      </div>
      <NavbarCollapse>
        {isAuthenticated && (
          <>
            <NavbarLink as={Link} href="/apps">Apps</NavbarLink>
          </>
        )}
      </NavbarCollapse>
    </Navbar>
  );
}